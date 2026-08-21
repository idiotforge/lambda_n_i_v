import requests, base64, random, pathlib, discord, asyncio, os, sqlite3, json, yt_dlp, typing
from discord.ext import commands
from discord.types import voice
from discord import voice
from apitokens import *
from .constants import *
from .hardcoded import *
from .text_util import *
from util import *
from queue import Queue

def tts(req_text: str, text_speaker: str = "en_us_001",
    filename: str = 'voice.mp3'):
  req_text = req_text.replace("&", "and")
  req_text = req_text.replace("#", "hashtag")
  req_text = req_text.replace("+", "plus")
  req_text = req_text.replace(" ", "+")
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
  def __init__(self) -> None:
    self.voice_index = 13
    self.tempo = 1.0
    self.pitch = 1.0
  def gen_ffmpeg_options(self) -> str:
    if self.pitch != 1.0 or self.tempo != 1.0:
      return f'-af "asetrate=24000*{self.pitch},atempo={self.tempo / self.pitch},aresample=24000"'
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
    hardcoded_file = attempt_hardcoded(text)
    if hardcoded_file:
      self.filename = 'tts/hardcoded/' + hardcoded_file + '.ogg'
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
async def join_vc(bot: discord.Bot, message: discord.Message):
  if message.guild:
    if message.guild.voice_client is None:
        if type(message.author) is discord.Member and message.author.voice and message.author.voice.channel:
          await message.author.voice.channel.connect()
          print(f'created queue for guild id {message.guild.id}')
          queues[message.guild.id] = TTSQueue(message.guild)
          await message.add_reaction('✅')
        else:
          await message.reply('hey idiot have you tried joining a vc first?')
class TTS(commands.Cog):
  def __init__(self, bot):
    self.bot = bot

  @commands.command()
  async def kill(self, ctx: commands.Context):
    if ctx.voice_client:
      await ctx.voice_client.disconnect(force=False)
  
  @commands.command()
  async def join(self, ctx: commands.Context):
    await join_vc(self, ctx.message)
  
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
  
  async def _set_float(self, ctx: commands.Context, _val, names: list[str]):
    val: float
    try:
      val = float(_val) or 1.0
    except ValueError:
      await ctx.message.reply('numbers please!')
      return
    if ctx.author.id not in user_preference:
      user_preference[ctx.author.id] = VoicePreference()
    reply = "set "
    for name in names:
      setattr(user_preference[ctx.author.id], name, val)
      reply += name + ', '
    reply = reply[:-2] + f' to `{val}`'
    await ctx.message.reply(reply)
  @commands.command()
  async def speed(self, ctx: commands.Context, *, _val):
    await self._set_float(ctx, _val, ['pitch', 'tempo'])
  @commands.command()
  async def pitch(self, ctx: commands.Context, *, _val):
    await self._set_float(ctx, _val, ['pitch'])
  @commands.command()
  async def tempo(self, ctx: commands.Context, *, _val):
    await self._set_float(ctx, _val, ['tempo'])
    
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
    if not is_message_valid(bot, message) or len(message.content) >= 300:
      return
    if message.guild:
      if message.guild.voice_client:
        if message.guild.voice_client.channel.id == message.channel.id:
          queue = queues.get(message.guild.id, None)
          if queue:
            txt = message.clean_content.strip()
            if 'youtube.com/watch?v=' in message.content or 'youtu.be/' in message.content:
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
      else:
        test_string = message.content.lower()
        if 'aniv' in test_string and 'hop on' in test_string:
          await join_vc(bot, message)