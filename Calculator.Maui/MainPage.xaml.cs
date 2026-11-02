using Calculator.ViewModels;

namespace Calculator
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new CalculatorViewModel();
        }
    }
}
