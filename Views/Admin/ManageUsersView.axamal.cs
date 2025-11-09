using Avalonia.Controls;
using Allva.Desktop.ViewModels.Admin;

namespace Allva.Desktop.Views.Admin;

public partial class ManageUsersView : UserControl
{
    public ManageUsersView()
    {
        InitializeComponent();
        DataContext = new ManageUsersViewModel();
    }
}