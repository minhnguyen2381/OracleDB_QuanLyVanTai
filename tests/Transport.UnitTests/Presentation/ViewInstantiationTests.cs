using System.Threading;
using System.Windows;
using Transport.Presentation.Wpf.Views.Controls;
using Transport.Presentation.Wpf.Views.Dialogs;
using Transport.Presentation.Wpf.Views.Pages;

namespace Transport.UnitTests.Presentation;

public class ViewInstantiationTests
{
    [Fact]
    public void AllPagesAndControlsCanBeInstantiatedWithoutXamlErrors()
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current is null)
                {
                    var app = new System.Windows.Application();
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml", UriKind.Absolute)
                    });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/Transport.Presentation.Wpf;component/Themes/Colors.xaml", UriKind.Absolute)
                    });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/Transport.Presentation.Wpf;component/Themes/Controls.xaml", UriKind.Absolute)
                    });
                }

                _ = new CrudToolbar();
                _ = new FormHeader();
                _ = new FormButtons();
                _ = new RoutesPage();
                _ = new VehiclesPage();
                _ = new DriversPage();
                _ = new PassengersPage();
                _ = new SchedulesPage();
                _ = new SellTicketPage();
                _ = new TicketsPage();
                _ = new StatisticsPage();
                _ = new DashboardPage();
                _ = new StaffPage();
                _ = new BackupPage();
                _ = new LoginAuditPage();
                _ = new RegisterDialog();
                _ = new ChangePasswordDialog();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(caught);
    }
}
