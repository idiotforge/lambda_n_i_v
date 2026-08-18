import pathlib

hardcoded: list[str] = []
dir = pathlib.Path('tts/hardcoded')
if dir.is_dir():
  for file in dir.iterdir():
    hardcoded.append(file.name.split('.')[0])