import random

def attempt_hardcoded(text: str) -> str | None:
  test_string = text.replace(' ', '')[:-1]
  
  if test_string == 'INTERLOPE':
    return 'interlope' + str(random.randrange(1, 5))
  
  test_string = test_string.lower()
  
  if test_string == 'proceed':
    return 'weird'
  
  if test_string == 'lawnfinder': test_string = 'dawnfinder'
  
  if (
    test_string == 'dawnfinder'
    or test_string == 'luigihousebutitmansion'
    or test_string == 'pacman'
      ): return test_string
  
  
  return