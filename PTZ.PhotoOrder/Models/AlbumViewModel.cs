using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PTZ.PhotoOrder.Models
{
    public class AlbumViewModel
    {
        public PhotoViewModel[] Photos { get; internal set; }
        public string AlbumName { get; internal set; }
    }
}
