import discord
def is_message_valid(bot: discord.Bot, message: discord.Message) -> bool:
  if (
    message.author == bot.user
    or message.type != discord.MessageType.default and message.type != discord.MessageType.reply
    or message.content.startswith(('$', 'λ', '.'))
  ): return False
  return True