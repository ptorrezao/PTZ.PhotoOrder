﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTZ.PhotoOrder.Models
{
    public class PhotoOrderConfig
    {
        public string Title { get; set; }
        public decimal FotoPrice { get; set; }
        public TrelloConfig Trello{ get; set; }
    }
}
