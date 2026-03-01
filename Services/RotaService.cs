using Microsoft.EntityFrameworkCore;
using Rota2.Data;
using Rota2.Models;
using System.Collections.Generic;
using System.Linq;

namespace Rota2.Services
{
    public class RotaService : IRotaService
    {
        private readonly AppDbContext _db;
        public RotaService(AppDbContext db)
        {
            _db = db;
        }

        public IEnumerable<Rota> GetAll()
        {
            return _db.Rotas
                .AsNoTracking()
                .Include(r => r.Shifts)
                .Include(r => r.RotaDoctors).ThenInclude(rd => rd.User)
                .Include(r => r.RotaAdmins).ThenInclude(ra => ra.User)
                .AsSplitQuery()
                .OrderBy(r => r.Id)
                .ToList();
        }

        public Rota? GetById(int id)
        {
            return _db.Rotas
                .Include(r => r.Shifts)
                .Include(r => r.RotaDoctors).ThenInclude(rd => rd.User)
                .Include(r => r.RotaAdmins).ThenInclude(ra => ra.User)
                .AsSplitQuery()
                .SingleOrDefault(r => r.Id == id);
        }

        public Rota Create(Rota rota, IEnumerable<int> doctorIds, IEnumerable<int> adminIds)
        {
            // detach any ids on shifts
            foreach (var s in rota.Shifts)
            {
                s.Id = 0;
            }
            _db.Rotas.Add(rota);
            _db.SaveChanges();
            foreach (var d in doctorIds.Distinct())
            {
                _db.RotaDoctors.Add(new RotaDoctor { RotaId = rota.Id, UserId = d });
            }
            foreach (var a in adminIds.Distinct())
            {
                _db.RotaAdmins.Add(new RotaAdmin { RotaId = rota.Id, UserId = a });
            }
            _db.SaveChanges();
            return rota;
        }

        public void Update(Rota rota, IEnumerable<int> doctorIds, IEnumerable<int> adminIds)
        {
            var existing = _db.Rotas.Include(r => r.Shifts).Include(r => r.RotaDoctors).Include(r => r.RotaAdmins).SingleOrDefault(r => r.Id == rota.Id);
            if (existing == null) return;
            existing.Name = rota.Name;

            // replace shifts
            _db.Shifts.RemoveRange(existing.Shifts);
            foreach (var s in rota.Shifts)
            {
                s.Id = 0;
                s.RotaId = existing.Id;
                _db.Shifts.Add(s);
            }

            // replace doctors
            _db.RotaDoctors.RemoveRange(existing.RotaDoctors);
            foreach (var d in doctorIds.Distinct())
            {
                _db.RotaDoctors.Add(new RotaDoctor { RotaId = existing.Id, UserId = d });
            }

            // replace admins
            _db.RotaAdmins.RemoveRange(existing.RotaAdmins);
            foreach (var a in adminIds.Distinct())
            {
                _db.RotaAdmins.Add(new RotaAdmin { RotaId = existing.Id, UserId = a });
            }

            _db.SaveChanges();
        }

        public void Delete(int id)
        {
            var existing = _db.Rotas.Find(id);
            if (existing == null) return;
            _db.Rotas.Remove(existing);
            _db.SaveChanges();
        }
    }
}
