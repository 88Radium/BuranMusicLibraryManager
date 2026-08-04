using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;

namespace Buran.Types {
    public static class BuranMessageBox {
        public static async Task Show(string message, string? caption = null) {
            var box = MessageBoxManager.GetMessageBoxStandard(caption?? "Informaiton", message, ButtonEnum.Ok);

            await box.ShowAsync();
        }

        public static async Task<bool> AskYesNo(string message) {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Frage", message, ButtonEnum.YesNo);

            var result = await box.ShowAsync();
            return result == ButtonResult.Yes;
        }
    }
}