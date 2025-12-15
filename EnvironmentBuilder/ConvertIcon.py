"""
Convert SVG to ICO file for Environment Builder
Requires: pip install cairosvg pillow
"""

import os
import sys

try:
    from PIL import Image
    import cairosvg
    import io
except ImportError:
    print("Installing required packages...")
    os.system("pip install cairosvg pillow")
    from PIL import Image
    import cairosvg
    import io

def svg_to_ico(svg_path, ico_path, sizes=[16, 32, 48, 64, 128, 256]):
    """Convert SVG to multi-resolution ICO file"""
    
    print(f"Converting {svg_path} to {ico_path}")
    
    # Read SVG file
    with open(svg_path, 'rb') as f:
        svg_data = f.read()
    
    images = []
    
    for size in sizes:
        print(f"  Creating {size}x{size} icon...")
        # Convert SVG to PNG at specified size
        png_data = cairosvg.svg2png(bytestring=svg_data, output_width=size, output_height=size)
        
        # Open as PIL Image
        img = Image.open(io.BytesIO(png_data))
        
        # Convert to RGBA if necessary
        if img.mode != 'RGBA':
            img = img.convert('RGBA')
        
        images.append(img)
    
    # Save as ICO with multiple sizes
    images[0].save(
        ico_path,
        format='ICO',
        sizes=[(img.width, img.height) for img in images],
        append_images=images[1:]
    )
    
    print(f"Successfully created {ico_path}")
    return True

if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    svg_path = os.path.join(script_dir, "EnvironmentBuilderApp", "Resources", "TestTree.svg")
    ico_path = os.path.join(script_dir, "EnvironmentBuilderApp", "Resources", "TestTree.ico")
    
    if not os.path.exists(svg_path):
        # Try alternate path
        svg_path = os.path.join(script_dir, "..", "TestTree.svg")
    
    if os.path.exists(svg_path):
        svg_to_ico(svg_path, ico_path)
    else:
        print(f"SVG file not found: {svg_path}")
        sys.exit(1)

