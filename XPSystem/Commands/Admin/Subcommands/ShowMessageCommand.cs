﻿namespace XPSystem.Commands.Admin.Subcommands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using CommandSystem;
    using NorthwoodLib.Pools;
    using PlayerRoles;
    using XPSystem.API;
    using XPSystem.Config.Events;

    public class ShowMessageCommand : ICommand
    {
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermissionLS("xps.showmessage"))
            {
                response = "You do not have permission (xps.showmessage) to run this command.";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "Usage: showmessage <key> (<subkeys>)";
                return false;
            }

            string key = arguments.At(0);
            var role = RoleTypeId.None;

            if (key.StartsWith("default_"))
                key = key.Substring(8);
            else if (XPPlayer.TryGet(sender, out var player))
                role = player.Role;

            var file = XPECManager.GetFile(key, role);
            if (file == null)
            {
                response = "No such XPEC file.";
                return false;
            }

            object[] subkeys = null;
            if (arguments.Count > 1)
            {
                List<object> subkeyList = new();
                var types = file.ParametersTypes;
                var subkeysStrings = arguments
                    .Skip(1)
                    .ToArray();

                for (int i = 0; i < subkeysStrings.Length; i++)
                {
                    var @string = subkeysStrings[i];
                    if (types.Length <= i)
                    {
                        subkeyList.Add(@string);
                        continue;
                    }

                    Exception e = null;
                    var argTypes = types[i];
                    foreach (var type in argTypes)
                    {
                        try
                        {
                            var converted = type.IsEnum ? Enum.Parse(type, @string, true) : Convert.ChangeType(@string, type);
                            if (converted != null)
                            {
                                subkeyList.Add(converted);
                                break;
                            }
                        }
                        catch (Exception e2)
                        {
                            e = e2;
                        }
                    }

                    response =
                        $"Could not convert argument at {i}: {@string} to any of the specified types, last error {e?.ToString() ?? "null"}";
                    return false;
                }

                subkeys = subkeyList.ToArray();
            }

            var item = file.Get(subkeys);
            if (item == null)
            {
                response = "Item null.";
                return false;
            }

            var sb = StringBuilderPool.Shared.Rent();
            foreach (var property in item.GetType().GetProperties())
            {
                sb.AppendLine($"{property.Name}: {property.GetValue(item)}");
            }

            response = StringBuilderPool.Shared.ToStringReturn(sb);
            return true;
        }

        public string Command { get; } = "showmessage";
        public string[] Aliases { get; } = { "sm" };
        public string Description { get; } = "Messaging and translation debug tool.";
    }
}