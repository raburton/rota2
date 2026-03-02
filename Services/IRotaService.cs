using Rota2.Models;
using System.Collections.Generic;

namespace Rota2.Services
{
    public interface IRotaService
    {
        IEnumerable<Rota> GetAll();
        Rota? GetById(int id);
        Rota Create(Rota rota, IEnumerable<int> doctorIds, IEnumerable<int> adminIds);
        void Update(Rota rota, IEnumerable<int> doctorIds, IEnumerable<int> adminIds);
        void Delete(int id);
        Rota DuplicateRota(int sourceRotaId, string newName);
        IEnumerable<Rota2.Models.ShiftAssignment> GetAssignments(int rotaId, DateTime start, DateTime end);
        void CreateAssignments(int rotaId, DateTime start, DateTime end);
        void UpdateAssignment(int assignmentId, int? userId);
        IEnumerable<Rota2.Models.ShiftAssignment> GetAssignmentsForUser(int userId, DateTime start, DateTime end);
    }
}
