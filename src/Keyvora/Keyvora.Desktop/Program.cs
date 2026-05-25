namespace Keyvora.Desktop;

using System;
using System.Threading;
using System.Windows;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        const string mutexName = "Keyvora.Desktop.SingleInstance";

        try
        {
            using var mutex = Mutex.OpenExisting(mutexName);
            MessageBox.Show("Keyvora est déjà en cours d'exécution.", "Keyvora",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        catch (AbandonedMutexException)
        {
            // Session précédente tuée — on continue
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // Aucune instance existante — ok
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
