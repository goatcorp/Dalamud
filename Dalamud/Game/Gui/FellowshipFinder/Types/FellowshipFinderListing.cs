using System;

using Dalamud.Data;
using Dalamud.Game.Gui.FellowshipFinder.Internal;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.GeneratedSheets;

namespace Dalamud.Game.Gui.FellowshipFinder.Types
{
    /// <summary>
    /// A single listing in fellowship finder.
    /// </summary>
    public class FellowshipFinderListing
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FellowshipFinderListing"/> class.
        /// </summary>
        /// <param name="listing">The interop listing data.</param>
        internal FellowshipFinderListing(FellowshipFinderPacketListing listing)
        {
            var dataManager = Service<DataManager>.Get();

            this.Id = listing.Id;
            this.Name = listing.Name;
            this.Comment = listing.Comment;

            this.MasterName = listing.MasterName;
            this.MasterWorld = new Lazy<World>(() => dataManager.GetExcelSheet<World>()!.GetRow(listing.MasterWorld));
            this.MasterWorldId = listing.MasterWorld;
            this.MasterContentIdLower = listing.MasterContentIdLower;

            this.RecruiterName = listing.RecruiterName;
            this.RecruiterWorld = new Lazy<World>(() => dataManager.GetExcelSheet<World>()!.GetRow(listing.RecruiterWorld));
            this.RecruiterWorldId = listing.RecruiterWorld;
            this.RecruiterContentIdLower = listing.RecruiterContentIdLower;

            this.RecruitmentDeadline = DateTime.UnixEpoch.AddSeconds(listing.Deadline);

            this.Members = listing.Members;
            this.TargetMembers = listing.Target;

            this.MainActivity = (FellowshipFinderActivity)listing.Activity1;
            this.SubActivity1 = (FellowshipFinderActivity)listing.Activity2;
            this.SubActivity2 = (FellowshipFinderActivity)listing.Activity3;

            this.LangsEnabled = listing.LangsEnabled;
            this.LangPrimary = listing.LangPrimary;
        }

        /// <summary>
        /// Gets the ID assigned to this listing by the game's server.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// Gets the name of this listing.
        /// </summary>
        public SeString Name { get; }

        /// <summary>
        /// Gets the comment for this recruitment listing.
        /// </summary>
        public SeString Comment { get; }

        /// <summary>
        /// Gets the name of the fellowship master.
        /// </summary>
        public SeString MasterName { get; }

        /// <summary>
        /// Gets the world of the fellowship master.
        /// </summary>
        public Lazy<World> MasterWorld { get; }

        /// <summary>
        /// Gets the world ID of the fellowship master.
        /// </summary>
        public uint MasterWorldId { get; }

        /// <summary>
        /// Gets the lower bits of the fellowship master's content ID.
        /// </summary>
        public uint MasterContentIdLower { get; }

        /// <summary>
        /// Gets the name of the fellowship recruiter.
        /// </summary>
        public SeString RecruiterName { get; }

        /// <summary>
        /// Gets the world of the fellowship recruiter.
        /// </summary>
        public Lazy<World> RecruiterWorld { get; }

        /// <summary>
        /// Gets the world ID of the fellowship recruiter.
        /// </summary>
        public uint RecruiterWorldId { get; }

        /// <summary>
        /// Gets the lower bits of the fellowship recruiter's content ID.
        /// </summary>
        public uint RecruiterContentIdLower { get; }

        /// <summary>
        /// Gets the deadline for recruitment.
        /// </summary>
        public DateTime RecruitmentDeadline { get; }

        /// <summary>
        /// Gets the number of members in this fellowship.
        /// </summary>
        public ushort Members { get; }

        /// <summary>
        /// Gets the number of members this fellowship would like to achieve.
        /// </summary>
        public ushort TargetMembers { get; }

        /// <summary>
        /// Gets the main activity of this fellowship.
        /// </summary>
        public FellowshipFinderActivity MainActivity { get; }

        /// <summary>
        /// Gets the first sub-activity of this fellowship.
        /// </summary>
        public FellowshipFinderActivity SubActivity1 { get; }

        /// <summary>
        /// Gets the second sub-activity of this fellowship.
        /// </summary>
        public FellowshipFinderActivity SubActivity2 { get; }

        /// <summary>
        /// Gets the bitmask of languages this fellowship uses.
        /// </summary>
        public uint LangsEnabled { get; }

        /// <summary>
        /// Gets the primary language of this fellowship.
        /// </summary>
        public uint LangPrimary { get; }
    }
}
