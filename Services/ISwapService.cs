using Rota2.Models;
using System.Collections.Generic;

namespace Rota2.Services
{
    public interface ISwapService
    {
        SwapRequest Create(SwapRequest req);
        IEnumerable<SwapRequest> GetIncoming(int userId);
        IEnumerable<SwapRequest> GetOutgoing(int userId);
        SwapRequest? GetById(int id);
        void UpdateStatus(int id, SwapStatus status);
        bool Accept(int id, int actingUserId);
    }
}
