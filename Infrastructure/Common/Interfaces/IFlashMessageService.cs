using Infrastructure.ViewModel;

namespace Infrastructure.Common.Interfaces
{
    /// <summary>
    /// Sets and consumes the one-shot <see cref="FlashMessageDto"/> stored in TempData.
    /// </summary>
    public interface IFlashMessageService
    {
        void Success(string title, string text);
        void Error(string title, string text);
        void Set(FlashMessageDto message);
        FlashMessageDto? Consume();
    }
}
