using System;
using System.Collections.Generic;
using System.Data.SQLite;
using MiniMuhasebe.Models;

namespace MiniMuhasebe.Data.Repositories
{
    public class FiscalPeriodRepository : BaseRepository<FiscalPeriod>
    {
        public FiscalPeriodRepository(string connectionString) : base(connectionString) { }

        public override FiscalPeriod GetById(int id)
        {
            string query = "SELECT * FROM FiscalPeriods WHERE PeriodId = @PeriodId";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@PeriodId", id)))
                {
                    if (reader.Read())
                        return MapFiscalPeriod(reader);
                }
            }
            return null;
        }

        public override List<FiscalPeriod> GetAll()
        {
            var periods = new List<FiscalPeriod>();
            string query = "SELECT * FROM FiscalPeriods ORDER BY StartDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection))
                {
                    while (reader.Read())
                        periods.Add(MapFiscalPeriod(reader));
                }
            }
            return periods;
        }

        public List<FiscalPeriod> GetByCompanyId(int companyId)
        {
            var periods = new List<FiscalPeriod>();
            string query = "SELECT * FROM FiscalPeriods WHERE CompanyId = @CompanyId ORDER BY StartDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CompanyId", companyId)))
                {
                    while (reader.Read())
                        periods.Add(MapFiscalPeriod(reader));
                }
            }
            return periods;
        }

        public List<FiscalPeriod> GetOpenPeriodsByCompanyId(int companyId)
        {
            var periods = new List<FiscalPeriod>();
            string query = "SELECT * FROM FiscalPeriods WHERE CompanyId = @CompanyId AND IsClosed = 0 ORDER BY StartDate DESC";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection, new SQLiteParameter("@CompanyId", companyId)))
                {
                    while (reader.Read())
                        periods.Add(MapFiscalPeriod(reader));
                }
            }
            return periods;
        }

        public FiscalPeriod GetCurrentPeriod(int companyId)
        {
            string query = @"SELECT * FROM FiscalPeriods 
                           WHERE CompanyId = @CompanyId AND IsClosed = 0 
                           AND StartDate <= @Today AND EndDate >= @Today 
                           ORDER BY StartDate DESC LIMIT 1";
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                using (var reader = ExecuteReader(query, connection,
                    new SQLiteParameter("@CompanyId", companyId),
                    new SQLiteParameter("@Today", DateTime.Today.ToString("yyyy-MM-dd"))))
                {
                    if (reader.Read())
                        return MapFiscalPeriod(reader);
                }
            }
            return null;
        }

        public override int Add(FiscalPeriod entity)
        {
            string query = @"INSERT INTO FiscalPeriods (CompanyId, PeriodName, StartDate, EndDate, IsClosed, CreatedAt, UpdatedAt)
                           VALUES (@CompanyId, @PeriodName, @StartDate, @EndDate, @IsClosed, @CreatedAt, @UpdatedAt)";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@CompanyId", entity.CompanyId),
                new SQLiteParameter("@PeriodName", entity.PeriodName),
                new SQLiteParameter("@StartDate", entity.StartDate.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@EndDate", entity.EndDate.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@IsClosed", entity.IsClosed ? 1 : 0),
                new SQLiteParameter("@CreatedAt", DateTime.Now),
                new SQLiteParameter("@UpdatedAt", DateTime.Now)
            };

            ExecuteNonQuery(query, parameters);
            return Convert.ToInt32(ExecuteScalar("SELECT last_insert_rowid()"));
        }

        public override bool Update(FiscalPeriod entity)
        {
            string query = @"UPDATE FiscalPeriods SET PeriodName = @PeriodName, StartDate = @StartDate, EndDate = @EndDate, 
                           IsClosed = @IsClosed, UpdatedAt = @UpdatedAt WHERE PeriodId = @PeriodId";

            var parameters = new SQLiteParameter[]
            {
                new SQLiteParameter("@PeriodName", entity.PeriodName),
                new SQLiteParameter("@StartDate", entity.StartDate.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@EndDate", entity.EndDate.ToString("yyyy-MM-dd")),
                new SQLiteParameter("@IsClosed", entity.IsClosed ? 1 : 0),
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@PeriodId", entity.PeriodId)
            };

            return ExecuteNonQuery(query, parameters) > 0;
        }

        public bool ClosePeriod(int periodId)
        {
            string query = "UPDATE FiscalPeriods SET IsClosed = 1, UpdatedAt = @UpdatedAt WHERE PeriodId = @PeriodId";
            return ExecuteNonQuery(query,
                new SQLiteParameter("@UpdatedAt", DateTime.Now),
                new SQLiteParameter("@PeriodId", periodId)) > 0;
        }

        public override bool Delete(int id)
        {
            string query = "DELETE FROM FiscalPeriods WHERE PeriodId = @PeriodId";
            return ExecuteNonQuery(query, new SQLiteParameter("@PeriodId", id)) > 0;
        }

        private FiscalPeriod MapFiscalPeriod(SQLiteDataReader reader)
        {
            return new FiscalPeriod
            {
                PeriodId = Convert.ToInt32(reader["PeriodId"]),
                CompanyId = Convert.ToInt32(reader["CompanyId"]),
                PeriodName = reader["PeriodName"].ToString(),
                StartDate = DateTime.Parse(reader["StartDate"].ToString()),
                EndDate = DateTime.Parse(reader["EndDate"].ToString()),
                IsClosed = Convert.ToInt32(reader["IsClosed"]) == 1,
                CreatedAt = DateTime.Parse(reader["CreatedAt"].ToString()),
                UpdatedAt = DateTime.Parse(reader["UpdatedAt"].ToString())
            };
        }
    }
}
