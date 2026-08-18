import io, markovify
#open('aniv/model.json', 'w').write(markovify.Text(open('aniv/set.txt').read(-1)).to_json())
model_json = open('aniv/model.json')
text_model = markovify.Text.from_json(model_json.read(-1))
model_json.close