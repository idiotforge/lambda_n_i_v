import io, markovify
model_json = open('aniv/model.json')
text_model = markovify.Text.from_json(model_json.read(-1))
model_json.close