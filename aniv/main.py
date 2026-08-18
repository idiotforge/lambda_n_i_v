import discord
from .model import *
global counter
counter: int = 5


async def on_message(bot: discord.Client, msg: discord.Message):
  global counter
  counter -= 1
  if 'nice one lambdaniv' in msg.content or 'good one lambdaniv' in msg.content:
    emoji = bot.get_emoji(1539305128920748042)
    if emoji:
      await msg.add_reaction(emoji)
  elif 'lambdaniv' in msg.content:
    await msg.reply(text_model.make_sentence())
  elif counter <= 0:
    counter = 5
    await msg.reply(text_model.make_sentence())
    