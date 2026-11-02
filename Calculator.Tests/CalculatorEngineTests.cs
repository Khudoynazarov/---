using System.Globalization;
using Calculator.Logic;

namespace Calculator.Tests;

[TestClass]
public sealed class CalculatorEngineTests
{
    private static CalculatorEngine Create() => new(CultureInfo.InvariantCulture);

    private static void Type(CalculatorEngine calc, string number)
    {
        foreach (var ch in number)
        {
            if (ch == '.')
                calc.EnterDecimalPoint();
            else
                calc.EnterDigit(ch - '0');
        }
    }

    [TestMethod]
    public void НовыйКалькулятор_ПоказываетНоль()
    {
        var calc = Create();
        Assert.AreEqual("0", calc.Display);
        Assert.AreEqual(0m, calc.CurrentValue);
        Assert.IsFalse(calc.HasError);
    }

    [TestMethod]
    public void ВводЦифр_ОбразуетЧисло()
    {
        var calc = Create();
        calc.EnterDigit(1);
        calc.EnterDigit(2);
        calc.EnterDigit(3);
        Assert.AreEqual("123", calc.Display);
        Assert.AreEqual(123m, calc.CurrentValue);
    }

    [TestMethod]
    public void ВедущиеНули_НеДобавляются()
    {
        var calc = Create();
        calc.EnterDigit(0);
        calc.EnterDigit(0);
        calc.EnterDigit(7);
        Assert.AreEqual("7", calc.Display);
    }

    [TestMethod]
    public void ДесятичнаяДробь_Вводится()
    {
        var calc = Create();
        Type(calc, "12.5");
        Assert.AreEqual("12.5", calc.Display);
        Assert.AreEqual(12.5m, calc.CurrentValue);
    }

    [TestMethod]
    public void ТочкаВНачале_ДаётНольТочка()
    {
        var calc = Create();
        calc.EnterDecimalPoint();
        calc.EnterDigit(5);
        Assert.AreEqual("0.5", calc.Display);
    }

    [TestMethod]
    public void ВтораяТочка_Игнорируется()
    {
        var calc = Create();
        Type(calc, "1.2");
        calc.EnterDecimalPoint();
        calc.EnterDigit(3);
        Assert.AreEqual("1.23", calc.Display);
    }

    [TestMethod]
    public void РусскаяКультура_РазделительЗапятая()
    {
        var calc = new CalculatorEngine(new CultureInfo("ru-RU"));
        Type(calc, "2.5");
        Assert.AreEqual("2,5", calc.Display);
        calc.SetOperation(Operation.Multiply);
        calc.EnterDigit(3);
        calc.Compute();
        Assert.AreEqual("7,5", calc.Display);
    }

    [TestMethod]
    public void ЧислоДлиннееЛимита_ЛишниеЦифрыИгнорируются()
    {
        var calc = Create();
        for (var i = 0; i < CalculatorEngine.MaxDigits + 5; i++)
            calc.EnterDigit(9);
        Assert.AreEqual(new string('9', CalculatorEngine.MaxDigits), calc.Display);
    }

    [TestMethod]
    public void НекорректнаяЦифра_ВыбрасываетИсключение()
    {
        var calc = Create();
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => calc.EnterDigit(10));
    }

    [TestMethod]
    [DataRow("2", Operation.Add, "3", "5")]
    [DataRow("10", Operation.Subtract, "4", "6")]
    [DataRow("3", Operation.Subtract, "8", "-5")]
    [DataRow("6", Operation.Multiply, "7", "42")]
    [DataRow("20", Operation.Divide, "4", "5")]
    [DataRow("1", Operation.Divide, "4", "0.25")]
    [DataRow("0.1", Operation.Add, "0.2", "0.3")]
    [DataRow("1.5", Operation.Multiply, "2", "3")]
    [DataRow("1", Operation.Divide, "3", "0.333333333333333")]
    public void БазовыеОперации_ДаютВерныйРезультат(string a, Operation op, string b, string expected)
    {
        var calc = Create();
        Type(calc, a);
        calc.SetOperation(op);
        Type(calc, b);
        calc.Compute();

        Assert.AreEqual(expected, calc.Display);
        Assert.IsFalse(calc.HasError);
    }

    [TestMethod]
    public void ДелениеИУмножение_НеТеряютТочность()
    {
        var calc = Create();
        calc.EnterDigit(1);
        calc.SetOperation(Operation.Divide);
        calc.EnterDigit(3);
        calc.Compute();
        calc.SetOperation(Operation.Multiply);
        calc.EnterDigit(3);
        calc.Compute();
        Assert.AreEqual("1", calc.Display);
    }

    [TestMethod]
    public void ЗаменаОперации_ИспользуетсяПоследняя()
    {
        var calc = Create();
        Type(calc, "9");
        calc.SetOperation(Operation.Add);
        calc.SetOperation(Operation.Subtract);
        Type(calc, "4");
        calc.Compute();
        Assert.AreEqual("5", calc.Display);
    }

    [TestMethod]
    public void ЦепочкаОпераций_ПоказываетПромежуточныйРезультат()
    {
        var calc = Create();
        Type(calc, "2");
        calc.SetOperation(Operation.Add);
        Type(calc, "3");
        calc.SetOperation(Operation.Multiply);
        Assert.AreEqual("5", calc.Display);
        Type(calc, "4");
        calc.Compute();
        Assert.AreEqual("20", calc.Display);
    }

    [TestMethod]
    public void ПослеРавно_РезультатСтановитсяПервымОперандом()
    {
        var calc = Create();
        Type(calc, "2");
        calc.SetOperation(Operation.Add);
        Type(calc, "3");
        calc.Compute();
        Assert.AreEqual("5", calc.Display);

        calc.SetOperation(Operation.Add);
        Type(calc, "4");
        calc.Compute();
        Assert.AreEqual("9", calc.Display);
    }

    [TestMethod]
    public void ПослеРавно_НовоеЧислоНачинаетНовоеВычисление()
    {
        var calc = Create();
        Type(calc, "2");
        calc.SetOperation(Operation.Add);
        Type(calc, "3");
        calc.Compute();

        Type(calc, "7");
        Assert.AreEqual("7", calc.Display);
        calc.SetOperation(Operation.Multiply);
        Type(calc, "2");
        calc.Compute();
        Assert.AreEqual("14", calc.Display);
    }

    [TestMethod]
    public void РавноБезВторогоОперанда_ИспользуетПервый()
    {
        var calc = Create();
        Type(calc, "5");
        calc.SetOperation(Operation.Add);
        calc.Compute();
        Assert.AreEqual("10", calc.Display);
    }

    [TestMethod]
    public void РавноБезОпераций_НичегоНеМеняет()
    {
        var calc = Create();
        Type(calc, "42");
        calc.Compute();
        Assert.AreEqual("42", calc.Display);
        Assert.IsFalse(calc.HasError);
    }

    [TestMethod]
    public void ПовторноеРавно_ПовторяетПоследнююОперацию()
    {
        var calc = Create();
        Type(calc, "5");
        calc.SetOperation(Operation.Add);
        Type(calc, "3");
        calc.Compute();
        Assert.AreEqual("8", calc.Display);
        calc.Compute();
        Assert.AreEqual("11", calc.Display);
        calc.Compute();
        Assert.AreEqual("14", calc.Display);
    }

    [TestMethod]
    public void ПовторноеРавно_ДляВычитанияИДеления()
    {
        var calc = Create();
        Type(calc, "100");
        calc.SetOperation(Operation.Divide);
        Type(calc, "2");
        calc.Compute();
        calc.Compute();
        Assert.AreEqual("25", calc.Display);

        calc.Clear();
        Type(calc, "10");
        calc.SetOperation(Operation.Subtract);
        Type(calc, "3");
        calc.Compute();
        calc.Compute();
        Assert.AreEqual("4", calc.Display);
    }

    [TestMethod]
    public void RepeatLastOperation_БезПредыдущейОперации_ВозвращаетFalse()
    {
        var calc = Create();
        Type(calc, "5");
        Assert.IsFalse(calc.RepeatLastOperation());
        Assert.AreEqual("5", calc.Display);
    }

    [TestMethod]
    public void RepeatLastOperation_ВызванныйНапрямую_Работает()
    {
        var calc = Create();
        Type(calc, "2");
        calc.SetOperation(Operation.Multiply);
        Type(calc, "3");
        calc.Compute();
        Assert.IsTrue(calc.RepeatLastOperation());
        Assert.AreEqual("18", calc.Display);
    }

    [TestMethod]
    public void ДелениеНаНоль_ПоказываетОшибку()
    {
        var calc = Create();
        Type(calc, "5");
        calc.SetOperation(Operation.Divide);
        Type(calc, "0");
        calc.Compute();

        Assert.IsTrue(calc.HasError);
        Assert.AreEqual(CalculatorEngine.DivideByZeroMessage, calc.Display);
    }

    [TestMethod]
    public void ДелениеНаНольВЦепочке_ПоказываетОшибку()
    {
        var calc = Create();
        Type(calc, "5");
        calc.SetOperation(Operation.Divide);
        Type(calc, "0");
        calc.SetOperation(Operation.Add);
        Assert.IsTrue(calc.HasError);
    }

    [TestMethod]
    public void ПослеОшибки_ПовторноеРавноНеПадаетИНичегоНеДелает()
    {
        var calc = Create();
        Type(calc, "5");
        calc.SetOperation(Operation.Divide);
        Type(calc, "0");
        calc.Compute();

        calc.Compute();
        calc.Compute();
        Assert.IsFalse(calc.RepeatLastOperation());
        Assert.AreEqual(CalculatorEngine.DivideByZeroMessage, calc.Display);
    }

    [TestMethod]
    public void ПослеОшибки_ВводЗаблокированДоСброса()
    {
        var calc = Create();
        Type(calc, "5");
        calc.SetOperation(Operation.Divide);
        Type(calc, "0");
        calc.Compute();

        calc.EnterDigit(7);
        calc.EnterDecimalPoint();
        calc.SetOperation(Operation.Add);
        Assert.IsTrue(calc.HasError);
        Assert.AreEqual(CalculatorEngine.DivideByZeroMessage, calc.Display);
    }

    [TestMethod]
    public void ПослеОшибкиИСброса_МожноСчитатьЗаново()
    {
        var calc = Create();
        Type(calc, "5");
        calc.SetOperation(Operation.Divide);
        Type(calc, "0");
        calc.Compute();

        calc.Clear();
        Assert.IsFalse(calc.HasError);
        Assert.AreEqual("0", calc.Display);

        Type(calc, "6");
        calc.SetOperation(Operation.Divide);
        Type(calc, "3");
        calc.Compute();
        Assert.AreEqual("2", calc.Display);
    }

    [TestMethod]
    public void Переполнение_ПоказываетОшибкуБезПадения()
    {
        var calc = Create();
        Type(calc, "9999999999999999");
        calc.SetOperation(Operation.Multiply);
        calc.Compute();
        Assert.IsTrue(calc.HasError);
        Assert.AreEqual(CalculatorEngine.OverflowMessage, calc.Display);
    }

    [TestMethod]
    public void Clear_СбрасываетВсёСостояние()
    {
        var calc = Create();
        Type(calc, "5");
        calc.SetOperation(Operation.Add);
        Type(calc, "3");
        calc.Compute();

        calc.Clear();

        Assert.AreEqual("0", calc.Display);
        Assert.IsNull(calc.PendingOperation);
        Assert.IsFalse(calc.RepeatLastOperation());
    }

    [TestMethod]
    public void Clear_ВоВремяВвода_СбрасываетОжидающуюОперацию()
    {
        var calc = Create();
        Type(calc, "5");
        calc.SetOperation(Operation.Add);
        Type(calc, "3");
        calc.Clear();
        Type(calc, "4");
        calc.Compute();
        Assert.AreEqual("4", calc.Display);
    }
}
