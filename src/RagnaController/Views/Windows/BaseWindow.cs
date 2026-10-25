using System.Windows;
using System.Windows.Input;

namespace RagnaController.Views.Windows
{
    /// <summary>
    /// TASK-TECH: Base Window Class.
    /// 
    /// Basis-Klasse für alle Window-Klassen mit DragMove-Unterstützung
    /// und gemeinsamen Fenster-Verhalten.
    /// Eliminiert DragMove-Duplikate in Window Code-Behind Dateien.
    /// </summary>
    public abstract class BaseWindow : Window
    {
        protected BaseWindow()
        {
            // Standard: Window per Maus ziehen (DragMove)
            MouseLeftButtonDown += BaseWindow_MouseLeftButtonDown;
        }

        /// <summary>
        /// Ermöglicht das Verschieben des Fensters per linker Maustaste.
        /// </summary>
        protected virtual void BaseWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        /// <summary>
        /// Schließt das Fenster.
        /// </summary>
        protected void CloseWindow()
        {
            Close();
        }
    }
}