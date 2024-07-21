using GBX.NET.Inputs;
using System.Text;

namespace GbxIo.Components.Tools;

public sealed class ExtractInputsTmiIoTool(string endpoint, IServiceProvider provider)
    : ExtractInputsIoTool(endpoint, provider)
{
    public override string Name => "Extract inputs (TMI)";

    protected override string Format => "python";

    protected override string CreateInputText(IEnumerable<IInput> inputs)
    {
        var sb = new StringBuilder();

        var start = 0;
        var axis = 1;
        var noSimplify = false;

        foreach (var input in inputs)
        {
            if (input is not Respawn or FakeFinishLine or FakeIsRaceRunning or FakeDontInverseAxis)
            {
                sb.Append(input.Time.TotalMilliseconds - 10 - start);
                sb.Append(' ');
            }

            switch (input)
            {
                case Accelerate accelerate:
                    sb.Append(accelerate.Pressed ? "press up" : "rel up");
                    break;
                case AccelerateReal accelerateReal:
                    sb.Append(accelerateReal.Value > 19660 ? "press up" : "rel up");
                    break;
                case Brake brake:
                    sb.Append(brake.Pressed ? "press down" : "rel down");
                    break;
                case BrakeReal brakeReal:
                    sb.Append(brakeReal.Value > 19660 ? "press down" : "rel down");
                    break;
                case Gas gas:
                    sb.Append("gas ");
                    sb.Append(gas.Value);
                    break;
                case Horn horn:
                    sb.Append("press horn");
                    break;
                case Respawn respawn:
                    if (respawn.Pressed && noSimplify)
                    {
                        sb.Append(input.Time.TotalMilliseconds - 10 - start);
                        sb.Append(" press enter");
                    }
                    else if (respawn.Pressed)
                    {
                        sb.Append((input.Time.TotalMilliseconds - 10 - start + 9) / 10 * 10);
                        sb.Append(" press enter");
                    }
                    else
                    {
                        sb.Append('#');
                        sb.Append(input.Time.TotalMilliseconds - 10);
                        sb.Append(" rel enter");
                    }
                    break;
                case RespawnTM2020 respawn:
                    sb.Append("press enter");
                    break;
                case SecondaryRespawn:
                    sb.Append("press enter");
                    break;
                case Steer steer:
                    sb.Append("steer ");
                    sb.Append(steer.Value * axis);
                    break;
                case SteerTM2020 steer2020:
                    sb.Append("steer ");
                    sb.Append(steer2020.Value * axis);
                    break;
                case SteerLeft steerLeft:
                    sb.Append(steerLeft.Pressed ? "press left" : "rel left");
                    break;
                case SteerRight steerRight:
                    sb.Append(steerRight.Pressed ? "press right" : "rel right");
                    break;
                case FakeFinishLine fakeFinish:
                    sb.Append('#');
                    sb.Append(input.Time.TotalMilliseconds - 10);
                    sb.Append(" fakeFinishLine");
                    break;
                case FakeIsRaceRunning fakeIsRaceRunning:
                    start = fakeIsRaceRunning.Time.TotalMilliseconds;
                    sb.Append('#');
                    sb.Append(start);
                    sb.Append(" fakeIsRaceRunning");
                    break;
                case FakeDontInverseAxis fakeDontInverseAxis:
                    axis *= -1;
                    sb.Append('#');
                    sb.Append(fakeDontInverseAxis.Time.TotalMilliseconds - 10);
                    sb.Append(" fakeDontInverseAxis");
                    break;
                default:
                    sb.Append('#');
                    sb.Append(input.Time.TotalMilliseconds - 10);
                    sb.Append(" unknown or unsupported");
                    break;
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
