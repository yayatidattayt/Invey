using Pocket.Core;
using Pocket.Core.Tracking;
using Xunit;
namespace Pocket.Core.Tests;
public class FastMotionRecoveryTests
{
 [Fact] public void Consistent_fast_motion_does_not_remain_a_permanent_spike()
 {
  var f=new PrecisionPointFilter();f.Apply(new(.1,.5),1000);f.Apply(new(.25,.5),1050);
  var output=f.Apply(new(.4,.5),1100);Assert.False(f.SpikePending);Assert.False(f.Uncertain);Assert.True(output.X>.25);
 }
 [Fact] public void Isolated_spike_and_reversing_outlier_do_not_become_motion()
 {
  var f=new PrecisionPointFilter();f.Apply(new(.1,.5),1000);f.Apply(new(.9,.5),1050);
  var p=f.Apply(new(.7,.5),1100);Assert.True(f.SpikePending);Assert.Equal(.1,p.X,6);
  p=f.Apply(new(.1,.5),1150);Assert.False(f.SpikePending);Assert.Equal(.1,p.X,6);
 }
}
