using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Buran.ID3Editor.ViewModels;

namespace Buran.ID3Editor.Views;

public partial class AddArtistToID3DialogWindow : Window {
    private AddArtistToID3DialogWindowViewModel ViewModel => DataContext as AddArtistToID3DialogWindowViewModel;

        public AddArtistToID3DialogWindow() {
            InitializeComponent();
            Loaded += OnWindowLoaded;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            ArtistTextBox.Focus();
        }

        private void ArtistTextBox_KeyDown(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter && ViewModel != null) {
                if(ViewModel.CanAddArtist) {
                    ViewModel.AddArtistCommand.Execute(null);
                    ArtistTextBox.Focus();
                    ArtistTextBox.SelectAll();
                    e.Handled = true;
                }
            } else if(e.Key == Key.Down && ViewModel?.AutoCompleteItems?.Count > 0) {
                // Navigiere zur AutoComplete-Liste
                AutoCompleteList.Focus();
                AutoCompleteList.SelectedIndex = 0;
                e.Handled = true;
            } else if(e.Key == Key.Up && AutoCompleteList.IsFocused) {
                // Zurück zur TextBox
                ArtistTextBox.Focus();
                e.Handled = true;
            }
        }

        private void AutoCompleteList_MouseDoubleClick(object sender, TappedEventArgs e) {
            if(ViewModel?.SelectedAutoCompleteItem != null) {
                ViewModel.NewArtistInput = ViewModel.SelectedAutoCompleteItem;
        
                // Direkt hinzufügen bei Doppelklick
                if(ViewModel.CanAddArtist) {
                    ViewModel.AddArtistCommand.Execute(null);
                    ArtistTextBox.Focus();
                    ArtistTextBox.SelectAll();
                }
        
                e.Handled = true;
            }
        }

        private void AutoCompleteList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(AutoCompleteList.SelectedItem != null && ViewModel != null) {
                // Bei Auswahl mit Pfeiltasten direkt in TextBox übernehmen
                ViewModel.NewArtistInput = AutoCompleteList.SelectedItem.ToString();
            }
        }

        // Handle Escape to close window
        protected override void OnKeyDown(KeyEventArgs e) {
            if(e.Key == Key.Escape && !AutoCompleteList.IsFocused) {
                ViewModel?.CancelCommand.Execute(null);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
}