using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// The availabiltiy of dubs or subtitles for an item.
    /// </summary>
    public enum DubAvailability
    {
        /// <summary>
        /// Fully available.
        /// </summary>
        Full,

        /// <summary>
        /// Partial availability.
        /// </summary>
        Partial,

        /// <summary>
        /// No availability.
        /// </summary>
        Missing
    }
}
