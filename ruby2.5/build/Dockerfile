FROM lambci/lambda-base:build

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_ruby2.5 \
    GEM_HOME=/var/runtime \
    GEM_PATH=/var/task/vendor/bundle/ruby/2.5.0:/opt/ruby/gems/2.5.0:/var/lang/lib/ruby/gems/2.5.0 \
    RUBYLIB=/var/task:/var/runtime/lib:/opt/ruby/lib \
    BUNDLE_SILENCE_ROOT_WARNING=1

RUN rm -rf /var/runtime /var/lang /var/rapid && \
  curl https://lambci.s3.amazonaws.com/fs/ruby2.5.tgz | tar -zx -C /

# Add these as a separate layer as they get updated frequently
# The pipx workaround is due to https://github.com/pipxproject/pipx/issues/218
RUN source /usr/local/pipx/shared/bin/activate && \
  pipx install awscli==1.* && \
  pipx install aws-lambda-builders==1.0.0 && \
  pipx install aws-sam-cli==1.0.0 && \
  gem update --system --no-document && \
  gem install --no-document bundler -v '~> 2.1'
