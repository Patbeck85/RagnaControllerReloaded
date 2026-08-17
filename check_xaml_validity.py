import os

src_dir = "/mnt/c/RagnaController/src/RagnaController"
xaml_files = [f for f in os.listdir(src_dir) if f.endswith('.xaml')]

print(f"Found {len(xaml_files)} XAML files in {src_dir}")

for filename in xaml_files:
    filepath = os.path.join(src_dir, filename)
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        # Basic validation - check for balanced tags
        open_tags = content.count('<')
        close_tags = content.count('>')
        if open_tags == close_tags:
            print(f"✓ {filename}: Valid XAML (balanced tags)")
        else:
            print(f"✗ {filename}: Invalid XAML (unbalanced tags - {open_tags} < vs {close_tags} >)")
    except Exception as e:
        print(f"✗ {filename}: Error - {e}")
