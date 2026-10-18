using System;
using System.Collections.Generic;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// Interface für Profile-Management.
    /// </summary>
    public interface IProfileManager
    {
        List<Profile> Profiles { get; }
        void Add(Profile profile);
        void Remove(string name);
        Profile? GetByName(string name);
        Profile? GetCurrent();
        void SetCurrent(Profile profile);
        bool LoadProfileAsync(string name);
        event EventHandler<Profile>? OnProfileChanged;
    }
}
