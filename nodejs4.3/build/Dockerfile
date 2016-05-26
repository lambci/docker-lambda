FROM lambci/lambda-base:build

ENV PATH=/usr/local/lib64/node-v4.3.x/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin \
    LD_LIBRARY_PATH=/usr/local/lib64/node-v4.3.x/lib:/lib64:/usr/lib64:/var/runtime:/var/task:/var/task/lib \
    NODE_PATH=/var/runtime:/var/task:/var/runtime/node_modules \
    npm_config_unsafe-perm=true

CMD ["npm", "rebuild"]
