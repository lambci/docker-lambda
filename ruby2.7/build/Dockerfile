FROM lambci/lambda-base-2:build

ENV PATH=/var/lang/bin:$PATH \
    LD_LIBRARY_PATH=/var/lang/lib:$LD_LIBRARY_PATH \
    AWS_EXECUTION_ENV=AWS_Lambda_ruby2.7 \
    GEM_HOME=/var/runtime \
    GEM_PATH=/var/task/vendor/bundle/ruby/2.7.0:/opt/ruby/gems/2.7.0:/var/lang/lib/ruby/gems/2.7.0 \
    RUBYLIB=/var/task:/var/runtime/lib:/opt/ruby/lib \
    BUNDLE_SILENCE_ROOT_WARNING=1

RUN rm -rf /var/runtime /var/lang /var/rapid && \
  curl https://lambci.s3.amazonaws.com/fs/ruby2.7.tgz | tar -zx -C /

# Add these as a separate layer as they get updated frequently
RUN pipx install awscli==1.* && \
  pipx install aws-lambda-builders==1.1.0 && \
  pipx install aws-sam-cli==1.2.0 && \
  gem update --system --no-document && \
  gem install --no-document bundler -v '~> 2.1'
