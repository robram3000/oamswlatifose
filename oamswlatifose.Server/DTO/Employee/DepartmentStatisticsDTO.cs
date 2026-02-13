namespace oamswlatifose.Server.DTO.Employee
{
    /// <summary>
    /// Department statistics data transfer object.
    /// </summary>
    public class DepartmentStatisticsDTO
    {
        public int TotalEmployees { get; set; }
        public List<DepartmentStatDTO> Departments { get; set; }
    }

    /// <summary>
    /// Individual department statistics.
    /// </summary>
    public class DepartmentStatDTO
    {
        public string DepartmentName { get; set; }
        public int EmployeeCount { get; set; }
        public int UniquePositions { get; set; }
        public int HasUserAccounts { get; set; }
        public double PercentageOfTotal { get; set; }
    }
}
