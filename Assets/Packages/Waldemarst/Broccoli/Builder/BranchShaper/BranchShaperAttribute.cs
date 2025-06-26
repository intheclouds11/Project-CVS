using System;

/// <summary>
/// Attribute to apply to Broccoli Branch Shaper implementations.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class BranchShaperAttribute : Attribute
{
    /// <summary>
    /// Shaper Id.
    /// </summary>
    public readonly int id;
    /// <summary>
    /// Order of display as a selectable shaper.
    /// </summary>
    public readonly int order;
    /// <summary>
    /// Name to display as a selectable shaper.
    /// </summary>
    public readonly string name;
    /// <summary>
    /// Set to false to hide this shaper, so that is no selectable for the user.
    /// </summary>
    public readonly bool exposed;
    /// <summary>
    /// BranchShaper class attribute.
    /// </summary>
    /// <param name="id">Shaper id.</param>
    /// <param name="name">Name to display as a selectable shaper.</param>
    /// <param name="order">Order of display as a selectable shaper.</param>
    /// <param name="exposed">Set to false to hide this shaper, so that is no selectable for the user.</param>
    public BranchShaperAttribute (int id, string name, int order = 0, bool exposed = true) {
        this.id = id;
        this.order = order;
        this.name = name;
        this.exposed = exposed;
    }
}