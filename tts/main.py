import requests, base64, random, pathlib, discord, asyncio, os, sqlite3
from discord.ext import commands
from apitokens import *
from .constants import *
from .hardcoded import *
from .util import *


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
  def __init__(self, client):
    self.client = client
  client: discord.VoiceClient
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
    self.client.stop()
    self.pop_voice()
    self.play()
  def play(self):
    if self.playing:
      return
    if len(self.voices) <= 0:
      return
    source = discord.PCMVolumeTransformer(discord.FFmpegPCMAudio(source=self.voices[0].filename, executable='ffmpeg'))
    self.playing = True
    self.client.play(source, after=lambda e: self._playnext())

global queues
queues: dict[int, TTSQueue] = {}
    

class TTS(commands.Cog):
  def __init__(self, bot):
    self.bot = bot

  @commands.command()
  async def kill(self, ctx: commands.Context):
    if ctx.voice_client:
      queues.pop(ctx.voice_client.channel.id) # type: ignore
      await ctx.voice_client.disconnect(force=False)
  
  @commands.command()
  async def join(self, ctx: commands.Context):
    if ctx.voice_client is None:
        if ctx.author.voice: # type: ignore
          await ctx.author.voice.channel.connect() # type: ignore
          queues[ctx.author.voice.channel.id] = TTSQueue(ctx.voice_client) # type: ignore
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
      await ctx.message.reply(f'invalid voice`')
    
async def on_message(bot: discord.Client, message: discord.Message):
  if message.type != discord.MessageType.default and message.type != discord.MessageType.reply:
    return
  if message.content.startswith(('$', 'λ')):
    return
  queue: TTSQueue | None = queues.get(message.channel.id, None)
  if queue:
    txt = message.clean_content.strip()
    attachments = message.attachments
    stickers = message.stickers
    txt = await textGoodizer(txt, attachments, stickers)
    if txt != '':
      voice = user_preference.get(message.author.id, 'en_us_001')
      queue.push_voice(TikTokVoice(txt, voice))
      queue.play()