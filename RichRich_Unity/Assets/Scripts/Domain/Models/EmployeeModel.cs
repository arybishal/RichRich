using System;

namespace RichRich.Domain.Models
{
    public enum EmployeeRole
    {
        // Auto Repair Workshop
        ApprenticeMechanic,
        SeniorTechnician,

        // Convenience Store
        StoreClerk,
        InventoryManager,

        // Digital Marketing Agency
        JuniorMarketer,
        SeniorStrategist
    }

    /// <summary>
    /// Commercial business employee domain model.
    /// Contributes productivity, handles operations, and incurs recurring monthly payroll.
    /// </summary>
    public class Employee
    {
        public string Id { get; }
        public string Name { get; }
        public string BusinessId { get; }
        public EmployeeRole Role { get; }
        public Currency MonthlySalary { get; }
        public double Productivity { get; private set; } // 1.0 = 100% standard baseline
        public GameDateTime HireDate { get; }
        public bool IsActive { get; private set; }

        public Employee(
            string id,
            string name,
            string businessId,
            EmployeeRole role,
            Currency monthlySalary,
            double productivity = 1.0,
            GameDateTime? hireDate = null,
            bool isActive = true)
        {
            Id = id ?? Guid.NewGuid().ToString("N");
            Name = name ?? throw new ArgumentNullException(nameof(name));
            BusinessId = businessId ?? throw new ArgumentNullException(nameof(businessId));
            Role = role;
            MonthlySalary = monthlySalary;
            Productivity = Math.Max(0.5, productivity);
            HireDate = hireDate ?? GameDateTime.DefaultStart;
            IsActive = isActive;
        }

        public void SetProductivity(double productivity)
        {
            Productivity = Math.Max(0.5, productivity);
        }

        public void Dismiss()
        {
            IsActive = false;
        }
    }
}
