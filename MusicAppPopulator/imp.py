import uuid

def generate_uuid():
    """Generate a UUID in the format c6ea7f12-7db0-418d-ace2-72a91fe2d8b4."""

    return str(uuid.uuid4())

print(generate_uuid())