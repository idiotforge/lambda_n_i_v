import discord, asyncio
from discord.ext import commands 
from apitokens import *
import tts.main as tts
import aniv.main as aniv
from typing import Optional

class MainCog(commands.Cog):
  def __init__(self, bot):
    self.bot = bot

class DumbBot(commands.Bot):
  pass
    
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

bot = DumbBot(
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
    bot.add_cog(MainCog(bot))
    bot.load_extension('tts.main')
    bot.load_extension('aniv.main')
    await bot.start(DISCORD_TOKEN)


asyncio.run(main())