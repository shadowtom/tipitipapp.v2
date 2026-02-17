using System.Threading.Tasks;
using tipitipapp.domain.Entities.Models;

namespace tipitipapp.Interfaces.Services
{
    public interface IPopupService
    {
        Task<TransactionResult> ShowNFCPopupAsync(decimal amount, string transactionType);
    }
}
