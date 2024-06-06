git init
git remote add origin https://github.com/Mallard64/Wumpus.git  
git pull origin master     


git status
git add .
git commit -m "Insert message here"
git push -u origin master

// refresh after updating .gitignore file
git rm -rf --cached .
git add .
 