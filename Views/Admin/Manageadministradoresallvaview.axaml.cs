using Avalonia.Controls;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin;

public partial class ManageAdministradoresAllvaView : UserControl
{
    public ManageAdministradoresAllvaView()
    {
        InitializeComponent();
        DataContext = new ManageAdministradoresAllvaViewModel();
    }
}