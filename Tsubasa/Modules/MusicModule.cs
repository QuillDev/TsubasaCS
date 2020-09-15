using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Tsubasa.Services;

namespace Tsubasa.Modules
{
    public class MusicModule : ModuleBase<ShardedCommandContext>
    {
        private readonly MusicService _music;

        public MusicModule(MusicService music)
        {
            _music = music;
        }
        
        [Command("join")]
        public async Task MusicJoin()
            => await _music.JoinAsync((SocketGuildUser)Context.User);

        [Command("play")]
        public async Task MusicPlay([Remainder]string search)
            => await ReplyAsync(embed: await _music.PlayAsync((SocketGuildUser)Context.User, search));

        [Command("leave")]
        public async Task MusicLeave()
            => await ReplyAsync(embed: await _music.LeaveAsync((SocketGuildUser)Context.User));

        [Command("queue")]
        public async Task MusicQueue()
            => await ReplyAsync(embed: await _music.ListAsync((SocketGuildUser)Context.User));

        [Command("skip")]
        public async Task SkipTrack()
            => await ReplyAsync(embed: await _music.SkipTrackAsync((SocketGuildUser)Context.User));

        [Command("volume")]
        public async Task Volume(int volume)
            => await ReplyAsync(embed: await _music.VolumeAsync((SocketGuildUser)Context.User, volume));
        
        [Command("crank it")]
        public async Task CrankIt()
            => await ReplyAsync(embed: await _music.VolumeAsync((SocketGuildUser)Context.User, 149));
        
        [Command("chill it")]
        public async Task ChillIt()
            => await ReplyAsync(embed: await _music.VolumeAsync((SocketGuildUser)Context.User, 40));

        [Command("Pause")]
        public async Task Pause()
            => await ReplyAsync(embed: await _music.Pause((SocketGuildUser)Context.User));

        [Command("Resume")]
        public async Task Resume()
            => await ReplyAsync(embed: await _music.Pause((SocketGuildUser)Context.User));
    }
    
    
}