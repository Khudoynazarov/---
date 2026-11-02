using System.Globalization;

namespace Calculator.Logic;

public enum Operation
{
    Add,
    Subtract,
    Multiply,
    Divide
}

public class CalculatorEngine
{
    public const int MaxDigits = 16;

    public const string DivideByZeroMessage = "Ошибка: деление на ноль";
    public const string OverflowMessage = "Ошибка: переполнение";

    private readonly CultureInfo _culture;

    private decimal _value;
    private string? _input;
    private decimal _accumulator;
    private Operation? _pendingOperation;
    private Operation? _lastOperation;
    private decimal _lastOperand;
    private bool _afterEquals;

    public CalculatorEngine() : this(CultureInfo.CurrentCulture)
    {
    }

    public CalculatorEngine(CultureInfo culture)
    {
        _culture = culture ?? throw new ArgumentNullException(nameof(culture));
        Clear();
    }

    public string Display =>
        ErrorMessage
        ?? (_input != null ? _input.Replace(".", DecimalSeparator) : Format(_value));

    public decimal CurrentValue => _value;

    public string? ErrorMessage { get; private set; }

    public bool HasError => ErrorMessage != null;

    public Operation? PendingOperation => _pendingOperation;

    public string DecimalSeparator => _culture.NumberFormat.NumberDecimalSeparator;

    public void EnterDigit(int digit)
    {
        if (digit < 0 || digit > 9)
            throw new ArgumentOutOfRangeException(nameof(digit), "Цифра должна быть от 0 до 9.");
        if (HasError)
            return;

        BeginInputIfNeeded();

        if (CountDigits(_input!) >= MaxDigits)
            return;

        if (_input == "0")
            _input = digit.ToString(CultureInfo.InvariantCulture);
        else
            _input += digit.ToString(CultureInfo.InvariantCulture);

        _value = ParseInput(_input!);
    }

    public void EnterDecimalPoint()
    {
        if (HasError)
            return;

        BeginInputIfNeeded();

        if (!_input!.Contains('.'))
            _input += ".";
    }

    public void SetOperation(Operation operation)
    {
        if (HasError)
            return;

        if (_pendingOperation != null && _input != null)
        {
            if (!TryApply(_accumulator, _pendingOperation.Value, _value, out var result))
                return;
            _accumulator = result;
            _value = result;
        }
        else if (_pendingOperation == null)
        {
            _accumulator = _value;
        }

        _pendingOperation = operation;
        _input = null;
        _afterEquals = false;
    }

    public void Compute()
    {
        if (HasError)
            return;

        if (_pendingOperation != null)
        {
            var operation = _pendingOperation.Value;
            var operand = _value;

            if (!TryApply(_accumulator, operation, operand, out var result))
                return;

            _value = result;
            _lastOperation = operation;
            _lastOperand = operand;
            _pendingOperation = null;
        }
        else if (_lastOperation != null)
        {
            RepeatLastOperation();
            return;
        }

        _input = null;
        _afterEquals = true;
    }

    public bool RepeatLastOperation()
    {
        if (HasError || _lastOperation == null)
            return false;

        var operation = _lastOperation.Value;
        var left = _value;

        if (!TryApply(left, operation, _lastOperand, out var result))
            return false;

        _value = result;
        _pendingOperation = null;
        _input = null;
        _afterEquals = true;
        return true;
    }

    public void Clear()
    {
        _value = 0;
        _input = null;
        _accumulator = 0;
        _pendingOperation = null;
        _lastOperation = null;
        _lastOperand = 0;
        _afterEquals = false;
        ErrorMessage = null;
    }

    private void BeginInputIfNeeded()
    {
        if (_input != null)
            return;

        if (_afterEquals)
        {
            _accumulator = 0;
            _pendingOperation = null;
            _afterEquals = false;
        }

        _input = "0";
        _value = 0;
    }

    private bool TryApply(decimal left, Operation operation, decimal right, out decimal result)
    {
        result = 0;
        try
        {
            switch (operation)
            {
                case Operation.Add:
                    result = left + right;
                    break;
                case Operation.Subtract:
                    result = left - right;
                    break;
                case Operation.Multiply:
                    result = left * right;
                    break;
                case Operation.Divide:
                    if (right == 0)
                    {
                        SetError(DivideByZeroMessage);
                        return false;
                    }
                    result = left / right;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
            return true;
        }
        catch (OverflowException)
        {
            SetError(OverflowMessage);
            return false;
        }
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        _input = null;
        _pendingOperation = null;
        _lastOperation = null;
    }

    private static decimal ParseInput(string input)
    {
        var text = input.TrimEnd('.');
        if (text.Length == 0)
            return 0;
        return decimal.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);
    }

    private static int CountDigits(string input) => input.Count(char.IsDigit);

    public string Format(decimal value)
    {
        if (value == 0)
            return "0";

        var integerDigits = Math.Truncate(Math.Abs(value)).ToString(CultureInfo.InvariantCulture).Length;
        if (integerDigits > MaxDigits)
            return value.ToString("0.###############E+0", _culture);

        var decimals = Math.Min(28, MaxDigits - integerDigits);
        var rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        if (rounded == 0)
            return value.ToString("0.###############E+0", _culture);

        return rounded.ToString("0.############################", _culture);
    }
}
