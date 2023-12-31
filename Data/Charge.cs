﻿using Microsoft.CodeAnalysis;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace _4PL.Data
{
    public class Charge
    {
        [Key] 
        public Guid Id { get; set; }
        public string Charge_Description { get; set; }
        public string Calculation_Base { get; set; }
        public decimal Min {  get; set; }
        
        //To add
        public decimal OS_Unit_Price { get; set; }

        //To add
        public string OS_Currency { get; set; }
        public decimal Unit_Price { get; set; }
        public string Currency { get; set; }
        public decimal Per_Percent { get; set; }
        public string Charge_Code { get; set; }

        public Charge()
        {
            this.Id = Guid.NewGuid();
            this.Charge_Description = "empty";
            this.Calculation_Base = "empty";
            this.Min = 0;
            this.OS_Unit_Price = 0;
            this.OS_Currency = "empty";
            this.Unit_Price = 0;
            this.Currency = "empty";
            this.Per_Percent = 0;
            this.Charge_Code = "empty";
        }

        public Charge(string Id, string charge_Description, string calculation_Base, decimal min, decimal OS_unit_Price, string OS_currency, decimal unit_Price, string currency, decimal per_Percent, string charge_Code)
        {
            this.Id = Guid.Parse(Id);
            this.Charge_Description = charge_Description;
            this.Calculation_Base = calculation_Base;
            this.Min = min;
            this.OS_Unit_Price = OS_unit_Price;
            this.OS_Currency = OS_currency;
            this.Unit_Price = unit_Price;
            this.Currency = currency;
            this.Per_Percent = per_Percent;
            this.Charge_Code = charge_Code;
        }
    }
}
