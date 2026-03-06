using System;

namespace MediaBrowser.Model.Announcements;

/// <summary>
/// Request model used to create or update an announcement.
/// </summary>
public class AnnouncementRequest
{
    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the announcement body text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this announcement is pinned.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Gets or sets the priority value. Higher values are sorted first.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the display order value. Lower values are sorted first.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets the UTC visibility start time. Null means immediately visible.
    /// </summary>
    public DateTime? StartDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC visibility end time. Null means no expiry.
    /// </summary>
    public DateTime? EndDateUtc { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this announcement is enabled.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
