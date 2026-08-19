import requests, base64, random, pathlib, discord, asyncio, os, sqlite3
from discord.ext import commands
from discord.types import voice
from discord import voice
from apitokens import *
from .constants import *
from .hardcoded import *
from .util import *
import json

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
    
global user_preference
user_preference: dict[int, str] = {}

class TikTokVoice():
  filename: str
  def __init__(self, text: str, voice: str):
    attempt_hardcoded = text.replace(' ', '')
    if attempt_hardcoded in hardcoded:
      self.filename = 'tts/hardcoded/' + attempt_hardcoded + '.ogg'
    else:
      self.filename = 'tts/temp/temp_' + str(random.randrange(0, 99999999)) + '.mp3'
      tts(text, voice, self.filename)
  def dispose(self):
    if self.filename.startswith('tts/hardcoded'):
      return
    os.remove(self.filename)

class TTSQueue():
  def __init__(self, guild):
    self.guild = guild
  guild: discord.Guild
  playing = False
  voices = []
  def push_voice(self, voice):
    self.voices.append(voice)
  def pop_voice(self):
    if len(self.voices) <= 0:
      return
    self.voices[0].dispose()
    self.voices = self.voices[1:]
  def _playnext(self):
    self.playing = False
    if self.guild.voice_client is None:
      return
    self.guild.voice_client.stop()
    self.pop_voice()
    self.play()
  def dispose(self):
    for voice in self.voices:
      voice.dispose()
    self.voices.clear()
  def play(self):
    print("wh")
    if self.playing:
      return
    print('whs')
    if len(self.voices) <= 0:
      return
    print('ou shi')
    if self.guild.voice_client is None:
      return
    source = discord.PCMVolumeTransformer(discord.FFmpegPCMAudio(source=self.voices[0].filename, executable='ffmpeg'))
    self.playing = True
    self.guild.voice_client.play(source, after=lambda e: self._playnext())

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
    if ctx.voice_client is None:
        if ctx.author.voice: # type: ignore
          await ctx.author.voice.channel.connect() # type: ignore
          print(f'created queue for guild id {ctx.guild.id}') # type: ignore
          queues[ctx.guild.id] = TTSQueue(ctx.guild) # type: ignore
          await ctx.message.reply('yo waddup')
        else:
          await ctx.message.reply('hey idiot have you tried joining a vc first?')
  
  @commands.command()
  async def voicelist(self, ctx: commands.Context):
    await ctx.message.reply('\n'.join(voices))
  
  @commands.command()
  async def voice(self, ctx: commands.Context, *, voice):
    voice = voice.strip()
    if voice in voices:
      user_preference[ctx.author.id] = voice
      await ctx.message.reply(f'set your voice to `{voice}`')
    else:
      await ctx.message.reply('invalid voice')
  @commands.command()
  async def skip(self, ctx: commands.Context):
    pass
    
def setup(bot: discord.Bot):
  print('loading module tts')
  bot.add_cog(TTS(bot))
  @bot.listen()
  async def on_voice_state_update(member: discord.Member, before: discord.VoiceState, after: discord.VoiceState):
    if member.id == bot.user.id: # type: ignore
      if before.channel is not None and after.channel is None:
        # on disconnect
        queues.pop(member.guild.id).dispose()
        print(f'cleared guild {member.guild.id}')
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
            attachments = message.attachments
            stickers = message.stickers
            txt = await textGoodizer(txt, attachments, stickers)
            if txt != '':
              voice = user_preference.get(message.author.id, 'en_us_001')
              queue.push_voice(TikTokVoice(txt, voice))
              queue.play()