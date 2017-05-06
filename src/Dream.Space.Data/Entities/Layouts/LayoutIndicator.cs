﻿using Dream.Space.Models.Indicators;
using Dream.Space.Models.Layourts;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dream.Space.Data.Entities.Layouts
{
    public class LayoutIndicator: ILayoutIndicatorEntity
    {
        public int Id { get; set; }
        public int LayoutId { get; set; }
        public int IndicatorId { get; set; }

        [NotMapped]
        public IIndicatorEntity Indicator { get; set; }

        [NotMapped]
        public string Name { get; set; }

    }
}
