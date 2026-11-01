using System;

namespace RichRich.Domain.Models
{
    public enum RepairCategory
    {
        Maintenance,   // Oil change, filters, fluid top-up (Level 1)
        Brakes,        // Brake pads, rotors, caliper servicing (Level 1)
        Diagnostics,   // OBD-II scanning, electrical faults, ECU tuning (Level 2)
        Powertrain     // Engine overhaul, transmission rebuild (Level 3)
    }

    /// <summary>
    /// Individual vehicle repair job in the Auto Repair Workshop.
    /// Tracks customer requirements, completion duration, parts expense, and repair revenue.
    /// </summary>
    public class VehicleRepairJob
    {
        public string JobId { get; }
        public string VehicleModel { get; }
        public string CustomerName { get; }
        public RepairCategory Category { get; }
        public int RequiredLevel { get; }
        public int RequiredMinutes { get; }
        public int ElapsedMinutes { get; private set; }
        public Currency PartsCost { get; }
        public Currency CustomerPay { get; }
        public Currency CustomerPayment => CustomerPay;
        public int ReputationGain { get; }
        public bool IsCompleted { get; private set; }
        public bool IsDelivered { get; private set; }

        public float ProgressNormalized => (float)Math.Clamp((double)ElapsedMinutes / RequiredMinutes, 0.0, 1.0);

        public VehicleRepairJob(
            string jobId,
            string vehicleModel,
            string customerName,
            RepairCategory category,
            int requiredLevel,
            int requiredMinutes,
            Currency partsCost,
            Currency customerPay,
            int reputationGain = 3,
            int elapsedMinutes = 0,
            bool isCompleted = false,
            bool isDelivered = false)
        {
            JobId = jobId ?? Guid.NewGuid().ToString("N");
            VehicleModel = vehicleModel ?? "Sedan Standard";
            CustomerName = customerName ?? "Walk-in Customer";
            Category = category;
            RequiredLevel = Math.Max(1, requiredLevel);
            RequiredMinutes = Math.Max(1, requiredMinutes);
            PartsCost = partsCost;
            CustomerPay = customerPay;
            ReputationGain = reputationGain;
            ElapsedMinutes = Math.Clamp(elapsedMinutes, 0, RequiredMinutes);
            IsCompleted = isCompleted;
            IsDelivered = isDelivered;
        }

        public bool Progress(int minutes)
        {
            if (IsCompleted || minutes <= 0) return false;

            ElapsedMinutes += minutes;
            if (ElapsedMinutes >= RequiredMinutes)
            {
                ElapsedMinutes = RequiredMinutes;
                IsCompleted = true;
                return true;
            }
            return false;
        }

        public void Complete()
        {
            ElapsedMinutes = RequiredMinutes;
            IsCompleted = true;
        }

        public void Deliver()
        {
            IsDelivered = true;
        }
    }

    /// <summary>
    /// Service bay in the Auto Repair Workshop.
    /// </summary>
    public class ServiceBay
    {
        public int BayId { get; }
        public VehicleRepairJob CurrentJob { get; private set; }
        public bool IsOccupied => CurrentJob != null && !CurrentJob.IsDelivered;

        public ServiceBay(int bayId, VehicleRepairJob currentJob = null)
        {
            BayId = bayId;
            CurrentJob = currentJob;
        }

        public bool TryAssignJob(VehicleRepairJob job)
        {
            if (IsOccupied) return false;
            CurrentJob = job;
            return true;
        }

        public void Clear()
        {
            CurrentJob = null;
        }
    }
}
