import discord, emoji
async def makeWordGood(word: str):
  if '://' in word:
    # link
    if 'gif' in word or 'webp' in word or 'tenor' in word:
      return 'a gif'
    return 'a ' + word.split('/')[2].split('.')[-2] + ' link'
  elif '<:' in word:
    # emoji
    return 'emoji ' + word[2:].split(':')[0]
  elif '@' in word:
    # mention
    return word.replace('@', 'at ')
  return word

async def textGoodizer(txt: str, attachments: list[discord.Attachment], stickers: list[discord.StickerItem]):
  if txt == '':
    if len(stickers) != 0:
      return 'sticker ' + stickers[0].name
    if len(attachments) == 1:
      return attachments[0].filename
    if len(attachments) != 0:
      return 'multiple files'
    return ''
  txt = emoji.demojize(txt, delimiters=('emoji ', ' '))
  txt = txt.replace('>', '> ')
  words = txt.split(' ')
  for k in range(len(words)):
    words[k] = await makeWordGood(words[k]) + ' '
  txt = str.join('', words).strip()
  txt = txt.replace('_', ' ')
  if len(attachments) == 1:
    return txt + ' ' + attachments[0].filename
  elif len(attachments) != 0:
    return txt + ' with multiple files'
  return txt + '.'