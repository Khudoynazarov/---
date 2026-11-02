using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Calculator.Logic;

namespace Calculator.ViewModels;

public class CalculatorViewModel : INotifyPropertyChanged
{
    private readonly CalculatorEngine _engine;
    private string _display = "0";

    public CalculatorViewModel() : this(new CalculatorEngine())
    {
    }

    public CalculatorViewModel(CalculatorEngine engine)
    {
        _engine = engine;

        DigitCommand = new Command<string>(
            d => Run(() => _engine.EnterDigit(int.Parse(d))), _ => !_engine.HasError);
        DecimalPointCommand = new Command(() => Run(_engine.EnterDecimalPoint), () => !_engine.HasError);
        OperationCommand = new Command<string>(
            op => Run(() => _engine.SetOperation(ParseOperation(op))), _ => !_engine.HasError);
        EqualsCommand = new Command(() => Run(_engine.Compute), () => !_engine.HasError);
        ClearCommand = new Command(() => Run(_engine.Clear));

        Display = _engine.Display;
    }

    public string Display
    {
        get => _display;
        private set
        {
            if (_display == value)
                return;
            _display = value;
            OnPropertyChanged();
        }
    }

    public string DecimalSeparator => _engine.DecimalSeparator;

    public ICommand DigitCommand { get; }
    public ICommand DecimalPointCommand { get; }
    public ICommand OperationCommand { get; }
    public ICommand EqualsCommand { get; }
    public ICommand ClearCommand { get; }

    private void Run(Action action)
    {
        action();
        Display = _engine.Display;

        ((Command)DigitCommand).ChangeCanExecute();
        ((Command)DecimalPointCommand).ChangeCanExecute();
        ((Command)OperationCommand).ChangeCanExecute();
        ((Command)EqualsCommand).ChangeCanExecute();
    }

    private static Operation ParseOperation(string symbol) => symbol switch
    {
        "+" => Operation.Add,
        "-" => Operation.Subtract,
        "*" => Operation.Multiply,
        "/" => Operation.Divide,
        _ => throw new ArgumentException($"Неизвестная операция: {symbol}", nameof(symbol))
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
