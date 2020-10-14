using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Tsubasa.Services.Music_Services;

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
        {
            await _music.JoinAsync((SocketGuildUser) Context.User);
        }

        [Command("play")]
        public async Task MusicPlay([Remainder] string search)
        {
            await ReplyAsync(embed: await _music.PlayAsync((SocketGuildUser) Context.User, search));
        }

        [Command("leave"), Alias("stop")]
        public async Task MusicLeave()
        {
            await ReplyAsync(embed: await _music.LeaveAsync((SocketGuildUser) Context.User));
        }

        [Command("clear")]
        public async Task ClearQueue()
        {
            await ReplyAsync(embed: await _music.ClearQueueAsync((SocketGuildUser) Context.User));
        }

        [Command("remove"), Alias("rm")]
        public async Task RemoveSongFromQueue(int position)
        {
            await ReplyAsync(embed: await _music.RemoveSongAsync((SocketGuildUser) Context.User, position));
        }
        
        [Command("queue")]
        public async Task MusicQueue()
        {
            await ReplyAsync(embed: await _music.ListAsync((SocketGuildUser) Context.User));
        }

        [Command("skip")]
        public async Task SkipTrack()
        {
            await ReplyAsync(embed: await _music.SkipTrackAsync((SocketGuildUser) Context.User));
        }
        
        [Command("volume"), Priority(0)]
        public async Task Volume()
        {
            await ReplyAsync(embed: await _music.VolumeAsync((SocketGuildUser) Context.User));
        }
        
        [Command("volume"), Priority(0)]
        public async Task Volume(int percent)
        {
            await ReplyAsync(embed: await _music.VolumeAsync((SocketGuildUser) Context.User, percent));
        }

        [Command("crank it")]
        public async Task CrankIt()
        {
            await ReplyAsync(embed: await _music.VolumeAsync((SocketGuildUser) Context.User, 149));
        }

        [Command("chill it")]
        public async Task ChillIt()
        {
            await ReplyAsync(embed: await _music.VolumeAsync((SocketGuildUser) Context.User, 40));
        }

        [Command("Pause")]
        public async Task Pause()
        {
            await ReplyAsync(embed: await _music.Pause((SocketGuildUser) Context.User));
        }

        [Command("Resume")]
        public async Task Resume()
        {
            await ReplyAsync(embed: await _music.Pause((SocketGuildUser) Context.User));
        }

        //TODO See if there's a better way to implement different keywords
        [Command("Seek")]
        public async Task Seek(int seconds)
        {
            await ReplyAsync(embed: await _music.SeekAsync((SocketGuildUser) Context.User, seconds));
        }

        [Command("Art")]
        public async Task Art()
        {
            await ReplyAsync(embed: await _music.GetTrackArt((SocketGuildUser) Context.User));
        }

        [Command("Playing")]
        public async Task Playing()
        {
            await ReplyAsync(embed: await _music.Playing((SocketGuildUser) Context.User));
        }

        [Command("Lyrics")]
        public async Task Lyrics()
        {
            await ReplyAsync(embed: await _music.Lyrics((SocketGuildUser) Context.User));
        }
    }
}