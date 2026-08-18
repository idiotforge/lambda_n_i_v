import discord, asyncio
from discord.ext import commands 
from apitokens import *
import tts.main as tts
import aniv.main as aniv
from typing import Optional

class dumbBot(commands.Bot):
  async def on_message(self, message: discord.Message):
    if message.author == self.user:
      return
    await tts.on_message(self, message)
    await aniv.on_message(self, message)
    await super().on_message(message)
  # async def on_voice_state_update(self, member: discord.Member, before: discord.VoiceState, after: discord.VoiceState):
  #   await tts.on_voice_state_update(self, member, before, after)
    
intents = discord.Intents.default()
intents.message_content = True
class WorseHelpCommand(commands.HelpCommand):
  async def command_callback(self, ctx: commands.Context, /, *, command: Optional[str] = None) -> None:
    await ctx.message.reply("""
## `commands start with $ or λ`
- help: print this list
- voice (voice_name): sets the voice
- voicelist: lists all voices
- join: joins ur current vc
- kill: leaves vc
""")

bot = dumbBot(
  command_prefix=commands.when_mentioned_or('$', 'λ'),
  description='nothing ever happens',
  intents=intents,
  help_command = WorseHelpCommand()
)

@bot.event
async def on_ready():
  # Tell the type checker that User is filled up at this point
  assert bot.user is not None
  print(f'started {bot.user} (ID: {bot.user.id})')

async def main():
  async with bot:
    await bot.add_cog(tts.TTS(bot))
    await bot.start(DISCORD_TOKEN)


asyncio.run(main())