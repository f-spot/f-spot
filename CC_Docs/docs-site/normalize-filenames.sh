#!/bin/bash

# Script to normalize all markdown filenames to lowercase with hyphens
# This works around case-insensitive filesystem issues

echo "Normalizing markdown filenames to lowercase..."

# Create a temporary directory
mkdir -p temp_rename

# Function to normalize filename
normalize_name() {
    echo "$1" | tr '[:upper:]' '[:lower:]' | sed 's/_/-/g'
}

# Find all markdown files and create a rename plan
find . -name "*.md" -not -name "index.md" | while read -r file; do
    dir=$(dirname "$file")
    filename=$(basename "$file")
    normalized=$(normalize_name "$filename")
    
    if [ "$filename" != "$normalized" ]; then
        echo "Planning rename: $file -> $dir/$normalized"
        echo "$file|$dir/$normalized" >> temp_rename/rename_plan.txt
    fi
done

# Execute renames using temporary files to avoid conflicts
if [ -f "temp_rename/rename_plan.txt" ]; then
    echo "Executing renames..."
    
    while IFS='|' read -r oldpath newpath; do
        if [ -f "$oldpath" ]; then
            temp_name="temp_rename/$(basename "$newpath").tmp"
            echo "Renaming: $oldpath -> $newpath"
            
            # Move to temp location first
            mv "$oldpath" "$temp_name"
            # Then move to final location
            mv "$temp_name" "$newpath"
        fi
    done < temp_rename/rename_plan.txt
    
    echo "Cleanup temporary files..."
    rm -rf temp_rename
    echo "Done! All filenames normalized."
else
    echo "No files need renaming."
    rm -rf temp_rename
fi