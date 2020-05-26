using System.Numerics;
using Dalamud.Game.ClientState;
using ImGuiNET;

namespace Dalamud.Interface
{
    internal class ConditionDebugWindow
    {
        private Condition condition;
        
        internal bool Enabled = false;

        private static readonly Vector2 DefaultWindowSize = new Vector2( 375, 500 );

        internal ConditionDebugWindow( Dalamud dalamud )
        {
            this.condition = dalamud.ClientState.Condition;
        }
        
        internal void Draw()
        {
            ImGui.SetNextWindowSize( DefaultWindowSize, ImGuiCond.FirstUseEver );
            
            ImGui.Begin( "Condition Debug", ref Enabled );
            
            #if DEBUG
            ImGui.Text($"ptr: {this.condition.conditionArrayBase.ToString("X16")}"  );
            #endif

            ImGui.Text( "Current Conditions:" );
            ImGui.Separator();

            bool didAny = false;

            for( var i = 0; i < Condition.MaxConditionEntries; i++ )
            {
                var typedCondition = ( ConditionFlag )i;
                var cond = this.condition[ typedCondition ];

                if( !cond )
                {
                    continue;
                }

                didAny = true;

                ImGui.Text( $"ID: {i} Enum: {typedCondition}" );
            }

            if( !didAny )
            {
                ImGui.Text( "None. Talk to a shop NPC or visit a market board to find out more!!!!!!!" );
            }
            
            ImGui.End();
        }
    }
}
