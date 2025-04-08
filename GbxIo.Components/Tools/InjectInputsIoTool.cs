﻿using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Inputs;
using GbxIo.Components.Data;
using GbxIo.Components.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace GbxIo.Components.Tools;

public class InjectInputsIoTool(string endpoint, IServiceProvider provider)
    : IoTool<(TextData InputData, BinData? TargetGbx), Gbx>(endpoint, provider)
{
    public override string Name => "Inject inputs";

    private static readonly Regex TimeRegex = new(@"^(\d+(\.\d+)?)$", RegexOptions.Compiled);
    private static readonly Regex InputRegex = new(@"^(Accelerate|AccelerateReal|Brake|BrakeReal|Gas|Horn|Respawn|RespawnTM2020|SecondaryRespawn|Steer|SteerTM2020|SteerLeft|SteerRight|FakeFinishLine|FakeIsRaceRunning|FakeDontInverseAxis)(\s+(.+))?$", RegexOptions.Compiled);

    public override async Task<Gbx> ProcessAsync((TextData InputData, BinData? TargetGbx) input, CancellationToken cancellationToken)
    {
        await ReportAsync("Parsing input text...", cancellationToken);

        var inputs = ParseInputText(input.InputData.Text);

        if (!inputs.Any())
        {
            throw new InvalidOperationException("No valid inputs found in the input text.");
        }

        await ReportAsync($"Parsed {inputs.Count} inputs.", cancellationToken);

        // If we have a target GBX file, inject inputs into it
        if (input.TargetGbx != null)
        {
            await ReportAsync("Injecting inputs into existing GBX file...", cancellationToken);
            return await InjectIntoExistingGbx(inputs, input.TargetGbx, cancellationToken);
        }

        // Otherwise, create a new ghost with the inputs
        await ReportAsync("Creating new ghost with inputs...", cancellationToken);
        var ghost = new CGameCtnGhost();
        ghost.Inputs = inputs.ToImmutableList();

        var gbx = new Gbx<CGameCtnGhost>(ghost);

        await ReportAsync("Created ghost with injected inputs.", cancellationToken);

        return gbx;
    }

    private List<IInput> ParseInputText(string text)
    {
        var inputs = new List<IInput>();
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip comments
            if (trimmedLine.StartsWith("#") || string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            var parts = trimmedLine.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 1)
            {
                continue;
            }

            // Try to parse the input
            IInput? input = null;

            // Check if this is a standard format (Type Value)
            var match = InputRegex.Match(trimmedLine);
            if (match.Success)
            {
                var inputType = match.Groups[1].Value;
                var value = match.Groups[4].Value;

                input = CreateInput(inputType, value);
            }
            // Check if this is TMI format (Time Action)
            else if (parts.Length >= 2 && TimeRegex.IsMatch(parts[0]))
            {
                var time = double.Parse(parts[0]);
                var action = string.Join(" ", parts.Skip(1));

                input = ParseTmiInput(time, action);
            }

            if (input != null)
            {
                inputs.Add(input);
            }
        }

        return inputs;
    }

    private IInput? CreateInput(string inputType, string value)
    {
        // Create the appropriate input type based on the string
        switch (inputType)
        {
            case "Accelerate":
                return new Accelerate { Pressed = bool.Parse(value) };
            case "AccelerateReal":
                return new AccelerateReal { Value = int.Parse(value) };
            case "Brake":
                return new Brake { Pressed = bool.Parse(value) };
            case "BrakeReal":
                return new BrakeReal { Value = int.Parse(value) };
            case "Gas":
                return new Gas { Value = int.Parse(value) };
            case "Horn":
                return new Horn { Pressed = true };
            case "Respawn":
                return new Respawn { Pressed = true };
            case "RespawnTM2020":
                return new RespawnTM2020();
            case "SecondaryRespawn":
                return new SecondaryRespawn();
            case "Steer":
                return new Steer { Value = (int)float.Parse(value) };
            case "SteerTM2020":
                return new SteerTM2020 { Value = (sbyte)float.Parse(value) };
            case "SteerLeft":
                return new SteerLeft { Pressed = bool.Parse(value) };
            case "SteerRight":
                return new SteerRight { Pressed = bool.Parse(value) };
            case "FakeFinishLine":
                return new FakeFinishLine();
            case "FakeIsRaceRunning":
                return new FakeIsRaceRunning();
            case "FakeDontInverseAxis":
                return new FakeDontInverseAxis();
            default:
                return null;
        }
    }

    private IInput? ParseTmiInput(double timeMs, string action)
    {
        // Parse TMI format inputs (used by the ExtractInputsTmiIoTool)
        IInput? input = null;

        // Convert time to TimeSpan
        var time = TimeSpan.FromMilliseconds(timeMs);

        if (action.StartsWith("press "))
        {
            var key = action.Substring(6);
            switch (key)
            {
                case "up":
                    input = new Accelerate { Pressed = true, Time = time };
                    break;
                case "down":
                    input = new Brake { Pressed = true, Time = time };
                    break;
                case "left":
                    input = new SteerLeft { Pressed = true, Time = time };
                    break;
                case "right":
                    input = new SteerRight { Pressed = true, Time = time };
                    break;
                case "enter":
                    input = new Respawn { Pressed = true, Time = time };
                    break;
                case "horn":
                    input = new Horn { Pressed = true, Time = time };
                    break;
            }
        }
        else if (action.StartsWith("rel "))
        {
            var key = action.Substring(4);
            switch (key)
            {
                case "up":
                    input = new Accelerate { Pressed = false, Time = time };
                    break;
                case "down":
                    input = new Brake { Pressed = false, Time = time };
                    break;
                case "left":
                    input = new SteerLeft { Pressed = false, Time = time };
                    break;
                case "right":
                    input = new SteerRight { Pressed = false, Time = time };
                    break;
            }
        }
        else if (action.StartsWith("steer "))
        {
            var value = float.Parse(action.Substring(6));
            input = new Steer { Value = (int)value, Time = time };
        }
        else if (action.StartsWith("gas "))
        {
            var value = int.Parse(action.Substring(4));
            input = new Gas { Value = value, Time = time };
        }

        return input;
    }

    private async Task<Gbx> InjectIntoExistingGbx(List<IInput> inputs, BinData targetGbx, CancellationToken cancellationToken)
    {
        // Parse the target GBX file
        using var ms = new MemoryStream(targetGbx.Data);
        var gbxService = Provider.GetRequiredService<GbxService>();
        var gbx = await gbxService.ParseGbxAsync(ms, false);

        if (gbx == null)
        {
            throw new InvalidOperationException("Failed to parse target GBX file.");
        }

        // Inject inputs based on the GBX type
        switch (gbx)
        {
            case Gbx<CGameCtnGhost> ghostGbx:
                await ReportAsync("Injecting inputs into ghost...", cancellationToken);
                ghostGbx.Node.Inputs = inputs.ToImmutableList();
                return ghostGbx;

            case Gbx<CGameCtnReplayRecord> replayGbx:
                await ReportAsync("Injecting inputs into replay...", cancellationToken);
                // Use reflection to set the read-only property
                var inputsField = typeof(CGameCtnReplayRecord).GetField("<Inputs>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (inputsField != null)
                {
                    inputsField.SetValue(replayGbx.Node, inputs.ToImmutableList());
                }
                else
                {
                    throw new InvalidOperationException("Cannot set Inputs property on CGameCtnReplayRecord - backing field not found.");
                }
                return replayGbx;

            default:
                throw new InvalidOperationException($"Cannot inject inputs into GBX file of type {gbx.GetType().Name}. Only Ghost.Gbx and Replay.Gbx are supported.");
        }
    }
}
