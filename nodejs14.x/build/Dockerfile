FROM lambci/lambda:nodejs14.x as node

FROM lambci/lambda-base-2:build

ENV PATH=/var/lang/bin:$PATH \
LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
AWS_EXECUTION_ENV=AWS_Lambda_nodejs14.x \
NODE_PATH=/opt/nodejs/node14/node_modules:/opt/nodejs/node_modules:/var/runtime/node_modules

COPY --from=node /var/runtime /var/runtime
COPY --from=node /var/lang /var/lang
COPY --from=node /var/rapid /var/rapid

# Add these as a separate layer as they get updated frequently
RUN pipx install awscli==1.* && \
pipx install aws-lambda-builders==1.2.0 && \
pipx install aws-sam-cli==1.15.0
