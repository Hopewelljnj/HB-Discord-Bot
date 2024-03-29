﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Bot.Modules.Games
{
    public static class AyyGame
    {
        public static async Task Game(Discord.MessageEventArgs e)
        {
            if (e.Channel.Id == 134267667245694976 && Storage.ayyscore > 0)
            {
                try
                {
                    string msg = e.Message.Text;
                    if (msg[0] == '/')
                        msg = msg.Substring(1);

                    if (!msg.ToLower().Replace(" ", "").EndsWith("ayy"))
                    {
                        var info = Tools.GetServerInfo(e.Server.Id);
                        string text = "get as long a chain of /ayy 's before it gets broken. High Score: {0} Current Score: {1}";
                        var oldayy = Storage.ayyscore;
                        Storage.ayyscore = 0;
                        await Tools.Reply(e.User, e.Channel, $"You failed! The chain has been broken. The score was: {oldayy}", true);
                        await e.Channel.Edit(e.Channel.Name, String.Format(text, info.ayyScore, Storage.ayyscore));
                    }
                }
                catch (Exception) { }
            }
        }
    }
}
