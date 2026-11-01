using System;
using System.Collections.Generic;

namespace RichRich.Domain.Models
{
    /// <summary>
    /// Housing accommodation tier specification.
    /// Defines monthly rental costs, move-in deposits, comfort rating,
    /// energy recovery rates, and tenant financial eligibility requirements.
    /// </summary>
    public class HousingTier
    {
        public string Id { get; }
        public string Name { get; }
        public int TierLevel { get; }
        public Currency MonthlyRent { get; }
        public Currency DepositCost { get; }
        public int ComfortRating { get; }
        public int EnergyRecoveryPerNight { get; }
        public Currency MinIncomeRequirement { get; }
        public Currency MinSavingsRequirement { get; }
        public string Description { get; }
        public IReadOnlyList<string> Features { get; }

        public HousingTier(
            string id,
            string name,
            int tierLevel,
            Currency monthlyRent,
            Currency depositCost,
            int comfortRating,
            int energyRecoveryPerNight,
            Currency minIncomeRequirement,
            Currency minSavingsRequirement,
            string description,
            IEnumerable<string> features = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TierLevel = Math.Clamp(tierLevel, 1, 5);
            MonthlyRent = monthlyRent;
            DepositCost = depositCost;
            ComfortRating = Math.Clamp(comfortRating, 0, 100);
            EnergyRecoveryPerNight = Math.Max(10, energyRecoveryPerNight);
            MinIncomeRequirement = minIncomeRequirement;
            MinSavingsRequirement = minSavingsRequirement;
            Description = description ?? string.Empty;
            Features = new List<string>(features ?? Array.Empty<string>());
        }

        public static List<HousingTier> GetDefaultTiers()
        {
            return new List<HousingTier>
            {
                new HousingTier(
                    "housing_shared_room",
                    "Shared Room",
                    tierLevel: 1,
                    monthlyRent: Currency.FromDollars(300.00),
                    depositCost: Currency.FromDollars(300.00),
                    comfortRating: 20,
                    energyRecoveryPerNight: 50,
                    minIncomeRequirement: Currency.Zero,
                    minSavingsRequirement: Currency.Zero,
                    description: "A modest bunk in a shared house. Minimal privacy, but maximum monthly savings.",
                    features: new[] { "Shared Kitchen", "Street Parking", "Low Monthly Overhead" }
                ),
                new HousingTier(
                    "housing_studio",
                    "Studio Apartment",
                    tierLevel: 2,
                    monthlyRent: Currency.FromDollars(600.00),
                    depositCost: Currency.FromDollars(600.00),
                    comfortRating: 45,
                    energyRecoveryPerNight: 70,
                    minIncomeRequirement: Currency.Zero,
                    minSavingsRequirement: Currency.Zero,
                    description: "Private compact metro studio. Self-contained bathroom and kitchenette.",
                    features: new[] { "Private Bathroom", "Compact Kitchenette", "Central Location" }
                ),
                new HousingTier(
                    "housing_one_bedroom",
                    "One-Bedroom Apartment",
                    tierLevel: 3,
                    monthlyRent: Currency.FromDollars(1100.00),
                    depositCost: Currency.FromDollars(1100.00),
                    comfortRating: 70,
                    energyRecoveryPerNight: 85,
                    minIncomeRequirement: Currency.FromDollars(2500.00),
                    minSavingsRequirement: Currency.FromDollars(5000.00),
                    description: "Spacious modern apartment with dedicated bedroom, living area, and balcony.",
                    features: new[] { "Separate Bedroom", "In-unit Laundry", "Air Conditioning", "Balcony" }
                ),
                new HousingTier(
                    "housing_luxury",
                    "Luxury High-Rise Apartment",
                    tierLevel: 4,
                    monthlyRent: Currency.FromDollars(2500.00),
                    depositCost: Currency.FromDollars(2500.00),
                    comfortRating: 90,
                    energyRecoveryPerNight: 100,
                    minIncomeRequirement: Currency.FromDollars(6000.00),
                    minSavingsRequirement: Currency.FromDollars(25000.00),
                    description: "Prestige high-rise residence with panoramic skyline views, concierge, and gym.",
                    features: new[] { "Concierge 24/7", "Resident Gym & Pool", "Skyline Views", "Secure Parking" }
                ),
                new HousingTier(
                    "housing_penthouse",
                    "Penthouse Suite",
                    tierLevel: 5,
                    monthlyRent: Currency.FromDollars(5500.00),
                    depositCost: Currency.FromDollars(5500.00),
                    comfortRating: 100,
                    energyRecoveryPerNight: 100,
                    minIncomeRequirement: Currency.FromDollars(12000.00),
                    minSavingsRequirement: Currency.FromDollars(60000.00),
                    description: "Palatial rooftop penthouse with private elevator, wrap-around terrace, and executive finishes.",
                    features: new[] { "Private Elevator", "Wrap-around Terrace", "Smart Home Automation", "Executive Lounge" }
                )
            };
        }
    }
}
