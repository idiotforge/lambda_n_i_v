import discord
from .model import *

def setup(bot: discord.Bot):
  print('loading module aniv')
  
  @bot.listen()
  async def on_message(message: discord.Message):
    if message.author == bot.user:
      return
    if message.type != discord.MessageType.default and message.type != discord.MessageType.reply:
      return
    teststring = message.content.lower()
    async def react_aniv():
      emoji = bot.get_emoji(1539305128920748042)
      if emoji:
        await message.add_reaction(emoji)
    async def react_fu():
      emoji = bot.get_emoji(1527897814980366357)
      if emoji:
        await message.add_reaction(emoji)
    if 'aniv' in teststring:
      if 'nice one' in teststring or 'good one' in teststring:
        await react_aniv()
      elif 'bad one' in teststring:
        await react_fu()
      else:
        await message.reply(text_model.make_short_sentence(max_chars=120, tries=10000))
    elif 'good bot' in teststring:
      await react_aniv()
    elif 'bad bot' in teststring:
      await react_fu()