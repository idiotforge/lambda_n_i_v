import requests, base64, random, pathlib, discord, asyncio, os, sqlite3, json, yt_dlp
from discord.ext import commands
from discord.types import voice
from discord import voice
from apitokens import *
from .constants import *
from .hardcoded import *
from .util import *
from queue import Queue

def tts(req_text: str, text_speaker: str = "en_us_001",
    filename: str = 'voice.mp3'):
  req_text = req_text.replace("+", "plus")
  req_text = req_text.replace(" ", "+")
  req_text = req_text.replace("&", "and")
  req_text = req_text.replace("ä", "ae")
  req_text = req_text.replace("ö", "oe")
  req_text = req_text.replace("ü", "ue")
  req_text = req_text.replace("ß", "ss")

  r = requests.post(
    f"{API_BASE_URL}?text_speaker={text_speaker}&req_text={req_text}&speaker_map_type=0&aid=1233",
    headers={
      'User-Agent': USER_AGENT,
      'Cookie': f'sessionid={TIKTOK_TOKEN}'
    }
  )

  if r.json()["message"] == "Couldn't load speech. Try again.":
    output_data = {"status": "Session ID is invalid", "status_code": 5}
    print(output_data)
    return output_data

  vstr = [r.json()["data"]["v_str"]][0]

  b64d = base64.b64decode(vstr)

  with open(filename, "wb") as out:
    out.write(b64d)

#youtube_dl.utils.bug_reports_message = lambda: ""
ytdl = yt_dlp.YoutubeDL(params={
  "format": "bestaudio/best",
    "outtmpl": "%(extractor)s-%(id)s-%(title)s.%(ext)s",
    "restrictfilenames": True,
    "noplaylist": True,
    "nocheckcertificate": True,
    "ignoreerrors": False,
    "logtostderr": False,
    "quiet": True,
    "no_warnings": True,
    "default_search": "auto",
    "source_address": (
        "0.0.0.0"
    ),
    "cookiefile": "cookies.txt"
})

class VoicePreference:
  voice_index = 13
  tempo = 1.0
  pitch = 1.0
  def gen_ffmpeg_options(self) -> str:
    if self.pitch != 1.0:
      return f'-filter:a "asetrate={24000*self.pitch},aresample=24000"'
    return ''
  
    
global user_preference
user_preference: dict[int, VoicePreference] = {}

class AudioSource():
  def dispose(self):
    pass
  async def get_source(self) -> discord.PCMVolumeTransformer | None:
    return

class YTDLSource(AudioSource):
  url: str
  def __init__(self, url: str):
    self.url = url

  async def get_source(self):
    data = await asyncio.to_thread(ytdl.extract_info, self.url, download=False)
    if data:
      url = data.get('url', None)
      if url:
        return discord.PCMVolumeTransformer(discord.FFmpegPCMAudio(url, options="-vn"))
    return

class TikTokVoice(AudioSource):
  filename: str
  ffmpeg_options : str
  def __init__(self, text: str, voice: VoicePreference):
    self.ffmpeg_options = voice.gen_ffmpeg_options()
    attempt_hardcoded = text.replace(' ', '')
    if attempt_hardcoded in hardcoded:
      self.filename = 'tts/hardcoded/' + attempt_hardcoded + '.ogg'
    else:
      self.filename = 'tts/temp/temp_' + str(random.randrange(0, 99999999)) + '.mp3'
      tts(text, voice_list[voice.voice_index], self.filename)
  def dispose(self):
    if self.filename.startswith('tts/hardcoded'):
      return
    os.remove(self.filename)
  async def get_source(self):
    return discord.PCMVolumeTransformer(discord.FFmpegPCMAudio(source=self.filename, executable='ffmpeg', options=self.ffmpeg_options))

class TTSQueue():
  def __init__(self, guild):
    self.guild = guild
    self.playing = False
    self.voices = Queue()
    self.current_voice = None
    self.source = None
  guild: discord.Guild
  playing: bool
  voices: Queue[AudioSource]
  current_voice: AudioSource | None
  source: discord.PCMVolumeTransformer | None
  def put_voice(self, voice):
    self.voices.put(voice)
  def get_voice(self):
    if self.voices.empty():
      self.current_voice = None
    self.current_voice = self.voices.get()
  def skip(self):
    self.playing = False
    self.dispose_source()
    if self.guild.voice_client:
      self.guild.voice_client.stop()
    if self.current_voice:
      self.current_voice.dispose()
      self.current_voice = None
    while not self.voices.empty():
      self.voices.get().dispose()
  def dispose_source(self):
    if self.source:
      self.source.cleanup()
  def _playnext(self):
    if self.guild.voice_client:
      self.guild.voice_client.stop()
    self.dispose_source()
    if self.current_voice:
      self.current_voice.dispose()
    self.playing = False
    asyncio.run(self.play())
  def dispose(self):
    self.skip()
  async def play(self):
    if self.playing:
      return
    if self.voices.empty():
      return
    if self.guild.voice_client is None:
      return
    self.get_voice()
    if self.current_voice:
      self.source = await self.current_voice.get_source()
      if self.source:
        self.playing = True
        self.guild.voice_client.play(self.source, after=lambda e: self._playnext())
  

global queues
queues: dict[int, TTSQueue] = {}

class TTS(commands.Cog):
  def __init__(self, bot):
    self.bot = bot

  @commands.command()
  async def kill(self, ctx: commands.Context):
    if ctx.voice_client:
      await ctx.voice_client.disconnect(force=False)
  
  @commands.command()
  async def join(self, ctx: commands.Context):
    if ctx.guild:
      if ctx.guild.voice_client is None:
          if type(ctx.author) is discord.Member and ctx.author.voice and ctx.author.voice.channel:
            await ctx.author.voice.channel.connect()
            print(f'created queue for guild id {ctx.guild.id}')
            queues[ctx.guild.id] = TTSQueue(ctx.guild)
            await ctx.message.reply('yo waddup')
          else:
            await ctx.message.reply('hey idiot have you tried joining a vc first?')
  
  @commands.command()
  async def voicelist(self, ctx: commands.Context):
    await ctx.message.reply('\n'.join(voice_list))
  
  @commands.command()
  async def voice(self, ctx: commands.Context, *, voice):
    voice = voice.strip()
    if voice in voice_list:
      voice_index = voice_list.index(voice)
      if ctx.author.id not in user_preference:
        user_preference[ctx.author.id] = VoicePreference()
      user_preference[ctx.author.id].voice_index = voice_index
      await ctx.message.reply(f'set your voice to `{voice}`')
    else:
      await ctx.message.reply('invalid voice')
  @commands.command()
  async def speed(self, ctx: commands.Context, *, _spd):
    spd = float(_spd) or 1.0
    if ctx.author.id not in user_preference:
      user_preference[ctx.author.id] = VoicePreference()
    user_preference[ctx.author.id].tempo = spd
    user_preference[ctx.author.id].pitch = spd
    await ctx.message.reply(f'set your speed to `{spd}`')
    
  @commands.command()
  async def skip(self, ctx: commands.Context):
    if ctx.guild:
      queue = queues.get(ctx.guild.id)
      if queue:
        queue.skip()
        await ctx.message.reply(f'skipping allat <:emoji_41:1527897814980366357>')
    
def setup(bot: discord.Bot):
  print('loading module tts')
  bot.add_cog(TTS(bot))
  @bot.listen()
  async def on_voice_state_update(member: discord.Member, before: discord.VoiceState, after: discord.VoiceState):
    if member.id == bot.user.id: # type: ignore
      if before.channel is not None and after.channel is None:
        # on disconnect
        queues.pop(before.channel.guild.id).dispose()
        print(f'cleared guild {before.channel.guild.id}')
  @bot.listen()
  async def on_message(message: discord.Message):
    if message.author == bot.user:
      return
    if message.type != discord.MessageType.default and message.type != discord.MessageType.reply:
      return
    if message.content.startswith(('$', 'λ', '.')):
      return
    if len(message.content) >= 300:
      return
    if message.guild:
      if message.guild.voice_client:
        if message.guild.voice_client.channel.id == message.channel.id:
          queue = queues.get(message.guild.id, None)
          if queue:
            txt = message.clean_content.strip()
            if 'https://www.youtube.com/watch?v=' in message.content:
              url = message.content.split()[0]
              queue.put_voice(YTDLSource(url))
              await queue.play()
            else:
              attachments = message.attachments
              stickers = message.stickers
              txt = await textGoodizer(txt, attachments, stickers)
              if txt != '':
                voice = user_preference.get(message.author.id, VoicePreference())
                queue.put_voice(TikTokVoice(txt, voice))
                await queue.play()